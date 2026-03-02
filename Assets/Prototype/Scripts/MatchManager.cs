using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public Unit[] Units => units;
    public GameEventBus EventBus => eventBus;

    [Header("Managers")]
    [SerializeField]
    private MapManager mapManager;

    [Header("Data")]
    [field: SerializeField]
    public TeamData TeamA { get; private set; }

    [field: SerializeField]
    public TeamData TeamB { get; private set; }

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

    public void ResetMatchData()
    {
        foreach (var ctx in teamContextTable.Values)
            ctx.Reset();
    }

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

    private void CheckWinCondition(TeamContext context)
    {
        if (context.Team == NeutralTeam) return;

        if (context.Score >= balanceConfig.TargetScore)
        {
            eventBus.Flow.PublishGameEnded(context.Team);
            Debug.Log($"Game Ended! Winner: {context.Team.name}");
        }
    }

    private void OnTimeExpired()
    {
        Debug.Log("Checking winner...");
        int scoreA = GetTeamContext(TeamA).Score;
        int scoreB = GetTeamContext(TeamB).Score;

        TeamData winner;
        if (scoreA > scoreB) winner = TeamA;
        else if (scoreB > scoreA) winner = TeamB;
        else winner = null;

        eventBus.Flow.PublishGameEnded(winner);
    }

    public IEnumerable<TeamContext> GetPlayableTeamContexts()
    {
        // 중립 제외
        if (teamContextTable.TryGetValue(TeamA, out var ctxA)) yield return ctxA;
        if (teamContextTable.TryGetValue(TeamB, out var ctxB)) yield return ctxB;
    }

    public TeamData OpponentTeam(TeamData team) => team.Opponent;

    public TeamContext GetTeamContext(TeamData team) =>
        teamContextTable.GetValueOrDefault(team, null);
}
