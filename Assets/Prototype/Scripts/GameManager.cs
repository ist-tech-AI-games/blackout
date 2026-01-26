using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MapManager mapManager;

    [Header("Data")]
    [SerializeField] private TeamData teamA;
    [SerializeField] private TeamData teamB;
    [SerializeField] private TeamData neutralTeam;
    [SerializeField] private UnitData defaultUnitData;

    [Header("Scene Objects")]
    [SerializeField] private Unit[] units;
    [SerializeField] private List<ItemObject> items = new();
    [SerializeField] private ItemObject itemPrefab;
    [SerializeField] private Transform itemParent;

    private Dictionary<TeamData, TeamContext> teamContextTable;

    void Awake()
    {
        teamContextTable = new()
        {
            {teamA, new(teamA)},
            {teamB, new(teamB)},
            {neutralTeam, new(neutralTeam)},
        };

        mapManager.Initialize();

        foreach (var unit in units)
            unit.Initialize(this, mapManager);
        foreach (var item in items)
            item.Initialize(this, mapManager);

        GetTeamContext(neutralTeam).OnScoreChanged += CheckEndGame;
    }

    public void SpawnItem(ItemData itemData, int amount, Vector2Int cellPos)
    {
        ItemObject instance = Instantiate(itemPrefab, itemParent);
        instance.transform.position = mapManager.CellToCenterWorld(cellPos);
        instance.SetData(itemData, amount);
        items.Add(instance);
    }

    public void RespawnUnit(Unit unit)
    {
        var spaceInfo = mapManager.MapSpaceInfo;
        if (unit.Team == teamA)
            unit.Teleport(spaceInfo.TeamASpawnPoint);
        else if (unit.Team == teamB)
            unit.Teleport(spaceInfo.TeamBSpawnPoint);
        else
        {
            Debug.LogError("Failed to Respawn; An unit should belong to 1 of 2 teams");
            return;
        }

        unit.SetUnitClass(defaultUnitData);
    }

    public Transform GetItemParent() => itemParent;

    public TeamData OpponentTeam(TeamData team) => team.Opponent;

    public TeamContext GetTeamContext(TeamData team) => teamContextTable.GetValueOrDefault(team, null);

    private void CheckEndGame(int remainingScore)
    {
        if (remainingScore <= 0) EndGame();
    }

    private void EndGame()
    {
        TeamData winner;

        int scoreA = GetTeamContext(teamA).Score;
        int scoreB = GetTeamContext(teamB).Score;

        if (scoreA > scoreB)
            winner = teamA;
        else if (scoreA < scoreB)
            winner = teamB;
        else
            winner = null;

        // TODO
        if (winner == null)
            Debug.Log("Draw!");
        else
            Debug.Log($"{winner.TeamName} win!");
    }
}
