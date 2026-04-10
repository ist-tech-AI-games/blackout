using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using SensorCompressionType = Unity.MLAgents.Sensors.SensorCompressionType;

/// <summary>
/// ML-Agents Agent for a single unit in Black Out.
/// 10 instances run in parallel (5 per team), sharing behavior name "BlackOutUnit".
/// Behavior Parameters: Vector Observation Size=RawObsSize(45), Continuous Actions=2, TeamId=0(A)/1(B).
/// </summary>
public class BlackOutAgent : Agent
{
    // Raw obs layout: 10 units × 4 floats + class(1) + scalars(3) + unitIndex(1) = 45
    private const int NUnits = 10;
    private const int UnitBlockSize = 4;
    private const int ScalarCount = 3;
    private const int RawObsSize = NUnits * UnitBlockSize + 1 + ScalarCount + 1;

    [SerializeField] private int unitIndex; // 0-4: Team A, 5-9: Team B

    private BlackOutEpisodeCoordinator coordinator;
    private GameScenario gameScenario;
    private MatchManager matchManager;
    private Unit unit;
    private TeamContext myTeamCtx;
    private TeamContext opponentCtx;
    private Action<int> onMyTeamScored;
    private Action<int> onOpponentScored;

    public int UnitIndex => unitIndex;

    /// <summary>
    /// Initializes agent references and subscribes to score events.
    /// Called once by <see cref="BlackOutEpisodeCoordinator"/> after <see cref="GameScenario.Initialize"/>.
    /// </summary>
    public void Setup(BlackOutEpisodeCoordinator episodeCoordinator, GameScenario scenario)
    {
        coordinator = episodeCoordinator;
        gameScenario = scenario;
        matchManager = scenario.MatchManager;
        unit = matchManager.Units[unitIndex];
        myTeamCtx = matchManager.GetTeamContext(unit.Team);
        opponentCtx = matchManager.GetTeamContext(matchManager.OpponentTeam(unit.Team));

        // NOTE: score > 0 guard prevents false reward when TeamContext.Reset() fires OnScoreChanged(0).
        onMyTeamScored = score => { if (score > 0) AddReward(0.1f); };
        onOpponentScored = score => { if (score > 0) AddReward(-0.1f); };
        myTeamCtx.OnScoreChanged += onMyTeamScored;
        opponentCtx.OnScoreChanged += onOpponentScored;

        // Assign the correct team-perspective RenderTexture to the sensor.
        // Must be done here (Awake phase) before Agent.OnEnable() calls InitializeSensors().
        var rtSensor = GetComponent<RenderTextureSensorComponent>();
        if (rtSensor != null)
        {
            SemanticMapRenderer renderer = episodeCoordinator.SemanticMapRenderer;
            rtSensor.RenderTexture = unit.Team == matchManager.TeamA
                ? renderer.RenderTextureTeamA
                : renderer.RenderTextureTeamB;
            rtSensor.Grayscale = true;
            rtSensor.CompressionType = SensorCompressionType.None;
        }
    }

    /// <summary>
    /// Notifies coordinator that this agent is ready.
    /// Coordinator starts a new game episode once all 10 agents have notified.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        coordinator?.NotifyAgentEpisodeBegin();
    }

    /// <summary>
    /// Collects RawObsSize-float observation vector.
    /// Layout: [0~39] per-unit block ×10 (absPos×2, teamSign, holdingItemId),
    ///         [40] self classId, [41~43] own score / opp score / time remaining,
    ///         [44] unitIndex (routing only — Python uses this to map agent_id, not passed to policy).
    /// absPos = (globalPos - mapOrigin) / mapBounds, normalized to [0,1].
    /// holdingItemId: 0=none, 1..N=KnownItems index+1 (Python converts to one-hot).
    /// classId: KnownClasses index as float (Python converts to one-hot).
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (matchManager == null || unit == null)
        {
            for (int i = 0; i < RawObsSize; i++) sensor.AddObservation(0f);
            return;
        }

        Vector2 mapOrigin = coordinator.MapOriginWorld;
        Vector2 bounds = coordinator.MapBounds;
        Unit[] units = matchManager.Units;

        foreach (Unit u in units)
        {
            sensor.AddObservation((u.GlobalPos - mapOrigin) / bounds);
            sensor.AddObservation(u.Team == unit.Team ? 1f : -1f);
            int itemIdx = u.HoldingItem != null ? coordinator.GetItemIndex(u.HoldingItem.ItemData) : -1;
            sensor.AddObservation(itemIdx >= 0 ? (float)(itemIdx + 1) : 0f);
        }

        sensor.AddObservation((float)coordinator.GetClassIndex(unit.UnitData));

        float targetScore = gameScenario.BalanceConfig.TargetScore;
        sensor.AddObservation(myTeamCtx.Score / targetScore);
        sensor.AddObservation(opponentCtx.Score / targetScore);
        sensor.AddObservation(gameScenario.EpisodeTimer != null ? 1f - gameScenario.EpisodeTimer.Ratio : 1f);

        // [RawObsSize-1] unitIndex — used by Python to map agent_id → unitIndex without assumptions.
        sensor.AddObservation((float)unitIndex);
    }

    private void FixedUpdate()
    {
        RequestDecision();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (gameScenario.CurrentState != GameState.Playing) return;
        gameScenario.MoveUnit(
            unitIndex,
            new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]),
            Time.fixedDeltaTime
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = UnityEngine.InputSystem.Keyboard.current != null
            ? (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed ? 1f :
               UnityEngine.InputSystem.Keyboard.current.aKey.isPressed ? -1f : 0f)
            : 0f;
        ca[1] = UnityEngine.InputSystem.Keyboard.current != null
            ? (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed ? 1f :
               UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ? -1f : 0f)
            : 0f;
    }

    private void OnDestroy()
    {
        if (myTeamCtx != null) myTeamCtx.OnScoreChanged -= onMyTeamScored;
        if (opponentCtx != null) opponentCtx.OnScoreChanged -= onOpponentScored;
    }
}
