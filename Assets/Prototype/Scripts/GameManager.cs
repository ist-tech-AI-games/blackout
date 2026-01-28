using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MapManager mapManager;

    [Header("Data")]
    [field: SerializeField] public TeamData TeamA { get; private set; }
    [field: SerializeField] public TeamData TeamB { get; private set; }
    [field: SerializeField] public TeamData NeutralTeam { get; private set; }
    [SerializeField] private UnitData defaultUnitData;

    [Header("Scene Objects")]
    [SerializeField] private Unit[] units;

    private Dictionary<TeamData, TeamContext> teamContextTable;

    void Awake()
    {
        teamContextTable = new()
        {
            {TeamA, new(TeamA)},
            {TeamB, new(TeamB)},
            {NeutralTeam, new(NeutralTeam)},
        };

        foreach (var unit in units)
            unit.Initialize(this, mapManager);
    }

    public void ResetAllUnits(MapData mapData)
    {
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

    public TeamData OpponentTeam(TeamData team) => team.Opponent;

    public TeamContext GetTeamContext(TeamData team) => teamContextTable.GetValueOrDefault(team, null);
}
