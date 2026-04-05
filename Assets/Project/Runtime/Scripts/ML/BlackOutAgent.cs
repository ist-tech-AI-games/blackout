using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// ML-Agents Agent for a single unit in Black Out.
/// 10 instances run in parallel (5 per team), sharing behavior name "BlackOutUnit".
/// Behavior Parameters: Vector Observation Size=46, Continuous Actions=2, TeamId=0(A)/1(B).
/// </summary>
public class BlackOutAgent : Agent
{
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
    /// Collects 46-float observation vector.
    /// Layout: [0-39] per-unit block ×10 (relPos×2, teamSign, holdingItem),
    ///         [40-42] self class one-hot, [43] own score, [44] opp score, [45] time remaining.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (matchManager == null || unit == null)
        {
            for (int i = 0; i < 46; i++) sensor.AddObservation(0f);
            return;
        }

        Vector2 bounds = coordinator.MapBounds;
        Unit[] units = matchManager.Units;

        foreach (Unit u in units)
        {
            sensor.AddObservation((u.GlobalPos - unit.GlobalPos) / bounds);
            sensor.AddObservation(u.Team == unit.Team ? 1f : -1f);
            sensor.AddObservation(u.HoldingItem != null ? 1f : 0f);
        }

        int classIdx = coordinator.GetClassIndex(unit.UnitData);
        for (int i = 0; i < coordinator.KnownClasses.Length; i++)
            sensor.AddObservation(i == classIdx ? 1f : 0f);

        float targetScore = gameScenario.BalanceConfig.TargetScore;
        sensor.AddObservation(myTeamCtx.Score / targetScore);
        sensor.AddObservation(opponentCtx.Score / targetScore);
        sensor.AddObservation(gameScenario.EpisodeTimer != null ? 1f - gameScenario.EpisodeTimer.Ratio : 1f);
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
        ca[0] = Input.GetAxis("Horizontal");
        ca[1] = Input.GetAxis("Vertical");
    }

    private void OnDestroy()
    {
        if (myTeamCtx != null) myTeamCtx.OnScoreChanged -= onMyTeamScored;
        if (opponentCtx != null) opponentCtx.OnScoreChanged -= onOpponentScored;
    }
}
