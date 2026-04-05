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

    [Tooltip("Unit classes in one-hot encoding order (Worker, Guard, Carrier).")]
    [SerializeField] private UnitData[] knownClasses;

    /// <summary>Gets the map size used to normalize per-unit relative positions.</summary>
    public Vector2 MapBounds => mapBounds;

    /// <summary>Gets the ordered unit class list for self class one-hot encoding.</summary>
    public UnitData[] KnownClasses => knownClasses;

    private int episodeBeginCount;

    private void Awake()
    {
        gameScenario.Initialize();

        foreach (var agent in agents)
            agent.Setup(this, gameScenario);

        gameScenario.EventBus.Flow.OnGameEnded += OnGameEnded;
    }

    private void Start()
    {
        gameScenario.EpisodeBegin();
    }

    private void FixedUpdate()
    {
        gameScenario.EpisodeUpdate(Time.fixedDeltaTime);
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

    private void OnGameEnded(TeamData winner)
    {
        MatchManager mm = gameScenario.MatchManager;

        foreach (var agent in agents)
        {
            Unit unit = mm.Units[agent.UnitIndex];
            float reward = winner == null ? 0f : winner == unit.Team ? 1f : -1f;
            agent.SetReward(reward);
            agent.EndEpisode();
        }
    }

    private void OnDestroy()
    {
        if (gameScenario?.EventBus != null)
            gameScenario.EventBus.Flow.OnGameEnded -= OnGameEnded;
    }
}
