using Unity.MLAgents.SideChannels;
using UnityEngine;

/// <summary>
/// Manages episode lifecycle for the 10-agent Black Out ML training setup.
/// Replaces <see cref="GameBootstrapper"/> in ML training scenes — do not use both simultaneously.
/// </summary>
public class BlackOutEpisodeCoordinator : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameScenario gameScenario;

    [Tooltip("All 10 BlackOutAgent components. Array index must match each agent's unitIndex.")]
    [SerializeField] private BlackOutAgent[] agents;

    [Header("Observation Settings")]
    [Tooltip("Playable map size in world units. Used to normalize relative positions in observations.")]
    [SerializeField] private Vector2 mapBounds = new Vector2(20f, 20f);

    [Tooltip("Unit classes in observation encoding order (Worker, Guard, Carrier). Index = classId float.")]
    [SerializeField] private UnitData[] knownClasses;

    [Tooltip("Known item types in holdingItemId encoding order. Index+1 = float ID (0=none).")]
    [SerializeField] private ItemData[] knownItems;

    [Tooltip("Semantic map renderer (shared, one per scene). Renders ally/enemy ID map per team.")]
    [SerializeField] private SemanticMapRenderer semanticMapRenderer;

    /// <summary>Gets the map size used to normalize absolute positions in observations.</summary>
    public Vector2 MapBounds => mapBounds;

    /// <summary>Gets the world-space bottom-left origin of the map for absPos normalization.</summary>
    public Vector2 MapOriginWorld => gameScenario.MapManager.MapOriginWorld;

    /// <summary>Gets the ordered unit class list for classId encoding.</summary>
    public UnitData[] KnownClasses => knownClasses;

    /// <summary>Gets the ordered item list for holdingItemId encoding.</summary>
    public ItemData[] KnownItems => knownItems;

    /// <summary>Gets the semantic map renderer used by all agents in this scene.</summary>
    public SemanticMapRenderer SemanticMapRenderer => semanticMapRenderer;

    private int episodeBeginCount;
    private SeedChannel _seedChannel;

    private void Awake()
    {
        // Allow Unity to run in background (required for standalone training builds).
        Application.runInBackground = true;

        // Cap to exactly 1 FixedUpdate per gRPC frame.
        // - Prevents FixedUpdate catch-up spiral (slow Python → flood → timeout).
        // - Prevents terminal/decision race at high time_scale: if game ends in FU1 and a
        //   new episode starts in FU2 of the same frame, Python only sees the decision obs
        //   and never receives the terminal signal → episode never terminates.
        Time.maximumDeltaTime = Time.fixedDeltaTime;

        // Must create RenderTextures before agent.Setup() so RenderTextureSensorComponent
        // can reference them during Agent.OnEnable() → InitializeSensors().
        semanticMapRenderer.CreateTextures();

        gameScenario.Initialize();

        _seedChannel = new SeedChannel();
        SideChannelManager.RegisterSideChannel(_seedChannel);

        foreach (var agent in agents)
            agent.Setup(this, gameScenario);

        semanticMapRenderer.SubscribeEvents(gameScenario.EventBus);
        gameScenario.EventBus.Flow.OnGameEnded += OnGameEnded;
    }

    private void Start()
    {
        gameScenario.EpisodeBegin();
    }

    private void FixedUpdate()
    {
        gameScenario.EpisodeUpdate(Time.fixedDeltaTime);
        semanticMapRenderer.Render();
    }

    /// <summary>
    /// Called by each <see cref="BlackOutAgent.OnEpisodeBegin"/>.
    /// Starts a new game episode once all agents have confirmed they are ready.
    /// </summary>
    public void NotifyAgentEpisodeBegin()
    {
        episodeBeginCount++;
        if (episodeBeginCount >= agents.Length)
        {
            episodeBeginCount = 0;
            gameScenario.EpisodeBegin();
        }
    }

    /// <summary>
    /// Returns the index of <paramref name="unitData"/> within <see cref="KnownClasses"/>, or -1 if not found.
    /// </summary>
    public int GetClassIndex(UnitData unitData)
    {
        if (knownClasses == null) return -1;
        for (int i = 0; i < knownClasses.Length; i++)
            if (knownClasses[i] == unitData) return i;
        return -1;
    }

    /// <summary>
    /// Returns the index of <paramref name="itemData"/> within <see cref="KnownItems"/>, or -1 if not found.
    /// Unity sends <c>index + 1</c> as the float holdingItemId (0 = no item).
    /// </summary>
    public int GetItemIndex(ItemData itemData)
    {
        if (knownItems == null) return -1;
        for (int i = 0; i < knownItems.Length; i++)
            if (knownItems[i] == itemData) return i;
        return -1;
    }

    private void OnGameEnded(TeamData winner)
    {
        MatchManager mm = gameScenario.MatchManager;

        foreach (var agent in agents)
        {
            Unit unit = mm.Units[agent.UnitIndex];
            float reward = winner == null ? 0f : winner == unit.Team ? 1f : -1f;
            agent.AddReward(reward);
            agent.EndEpisode();
        }
    }

    private void OnDestroy()
    {
        if (gameScenario?.EventBus != null)
            gameScenario.EventBus.Flow.OnGameEnded -= OnGameEnded;

        if (_seedChannel != null)
            SideChannelManager.UnregisterSideChannel(_seedChannel);
    }
}
