using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages match state, team contexts, and unit lifecycles.
/// Coordinates teams (A, B, Neutral), checks win conditions, and handles unit spawning/respawning.
/// Subscribes to GameEventBus for time expiration and score changes.
/// </summary>
public class MatchManager : MonoBehaviour
{
    /// <summary>
    /// All units participating in the match.
    /// </summary>
    public Unit[] Units => units;

    /// <summary>
    /// Event bus for publishing and subscribing to game events.
    /// </summary>
    public GameEventBus EventBus => eventBus;

    [Header("Managers")]
    [SerializeField]
    private MapManager mapManager;

    [Header("Data")]
    /// <summary>
    /// First playable team in the match.
    /// </summary>
    [field: SerializeField]
    public TeamData TeamA { get; private set; }

    /// <summary>
    /// Second playable team in the match (opponent of Team A).
    /// </summary>
    [field: SerializeField]
    public TeamData TeamB { get; private set; }

    /// <summary>
    /// Neutral team that doesn't participate in score-based win conditions.
    /// Used for neutral regions and objects.
    /// </summary>
    [field: SerializeField]
    public TeamData NeutralTeam { get; private set; }

    [SerializeField]
    private UnitData defaultUnitData;

    [Header("Scene Objects")]
    [SerializeField]
    private Unit[] units;

    private Dictionary<TeamData, TeamContext> teamContextTable;
    private GameEventBus eventBus;
    private GameBalanceConfig balanceConfig;
    private bool gameEnded;

    /// <summary>
    /// Initializes match manager with game scenario settings.
    /// Creates team contexts, subscribes to score/time events, and initializes all units.
    /// </summary>
    /// <param name="scenario">Game scenario containing event bus and balance configuration.</param>
    public void Initialize(GameScenario scenario)
    {
        eventBus = scenario.EventBus;
        balanceConfig = scenario.BalanceConfig;
        teamContextTable = new()
        {
            { TeamA, new(TeamA) },
            { TeamB, new(TeamB) },
            { NeutralTeam, new(NeutralTeam) },
        };

        foreach (var ctx in teamContextTable.Values)
            ctx.OnScoreChanged += (score) => CheckWinCondition(ctx);

        foreach (var unit in units)
            unit.Initialize(this, mapManager);

        eventBus.Flow.OnTimeExpired += OnTimeExpired;
    }

    /// <summary>
    /// Resets all team contexts (scores and modifiers).
    /// Called at the start of each episode.
    /// </summary>
    public void ResetMatchData()
    {
        gameEnded = false;
        foreach (var ctx in teamContextTable.Values)
            ctx.Reset();
    }

    /// <summary>
    /// Resets all units to their spawn positions with default class.
    /// Also resets match data (scores and modifiers).
    /// </summary>
    /// <param name="mapData">Map data containing spawn point information.</param>
    public void ResetAllUnits(MapData mapData)
    {
        ResetMatchData();
        foreach (var unit in units)
        {
            unit.ResetState();

            // 위치 이동
            if (unit.Team == TeamA)
                unit.Teleport(mapData.MapSpaceInfo.TeamASpawnPoint);
            else if (unit.Team == TeamB)
                unit.Teleport(mapData.MapSpaceInfo.TeamBSpawnPoint);

            unit.SetUnitClass(defaultUnitData);
        }
    }

    /// <summary>
    /// Respawns a unit at its team's spawn point with default class.
    /// Called when a unit dies during gameplay.
    /// </summary>
    /// <param name="unit">The unit to respawn.</param>
    public void RespawnUnit(Unit unit)
    {
        var spaceInfo = mapManager.MapSpaceInfo;
        if (unit.Team == TeamA)
            unit.Teleport(spaceInfo.TeamASpawnPoint);
        else if (unit.Team == TeamB)
            unit.Teleport(spaceInfo.TeamBSpawnPoint);
        else
        {
            Debug.LogError("Failed to Respawn; An unit should belong to 1 of 2 teams");
            return;
        }

        unit.SetUnitClass(defaultUnitData);
    }

    /// <summary>
    /// Checks if a team has reached the target score and publishes game end event.
    /// Called automatically when any team's score changes.
    /// </summary>
    /// <param name="context">Team context to check win condition for.</param>
    private void CheckWinCondition(TeamContext context)
    {
        if (context.Team == NeutralTeam) return;
        if (gameEnded) return;

        if (context.Score >= balanceConfig.TargetScore)
        {
            gameEnded = true;
            eventBus.Flow.PublishGameEnded(context.Team);
            Debug.Log($"Game Ended! Winner: {context.Team.name}");
        }
    }

    /// <summary>
    /// Handles time expiration event by determining winner based on scores.
    /// Publishes game ended event with winner (or null for tie).
    /// </summary>
    private void OnTimeExpired()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("Checking winner...");
        int scoreA = GetTeamContext(TeamA).Score;
        int scoreB = GetTeamContext(TeamB).Score;

        TeamData winner;
        if (scoreA > scoreB) winner = TeamA;
        else if (scoreB > scoreA) winner = TeamB;
        else winner = null;

        eventBus.Flow.PublishGameEnded(winner);
    }

    /// <summary>
    /// Gets all playable team contexts (Team A and Team B, excluding Neutral).
    /// </summary>
    /// <returns>Enumerable of playable team contexts.</returns>
    public IEnumerable<TeamContext> GetPlayableTeamContexts()
    {
        // 중립 제외
        if (teamContextTable.TryGetValue(TeamA, out var ctxA)) yield return ctxA;
        if (teamContextTable.TryGetValue(TeamB, out var ctxB)) yield return ctxB;
    }

    /// <summary>
    /// Gets the opponent team of the given team.
    /// </summary>
    /// <param name="team">Team to get opponent for.</param>
    /// <returns>Opponent team data.</returns>
    public TeamData OpponentTeam(TeamData team) => team.Opponent;

    /// <summary>
    /// Gets the team context for a given team.
    /// </summary>
    /// <param name="team">Team data to get context for.</param>
    /// <returns>Team context, or null if team is not registered.</returns>
    public TeamContext GetTeamContext(TeamData team) =>
        teamContextTable.GetValueOrDefault(team, null);
}
