using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private MapGenerator mapGenerator;

    [SerializeField]
    private TeamData teamA;

    [SerializeField]
    private TeamData teamB;

    [SerializeField]
    private TeamData neutralTeam;

    [SerializeField]
    private Tilemap tilemap;

    [SerializeField]
    private Unit[] units;
    [SerializeField]
    private ItemObject[] items;

    private MapData mapData;
    private Dictionary<TeamData, TeamContext> teamContextTable;

    void Awake()
    {
        teamContextTable = new()
        {
            {teamA, new(teamA)},
            {teamB, new(teamB)},
            {neutralTeam, new(neutralTeam)},
        };
        PrepareMap();
    }

    private void PrepareMap()
    {
        mapData = mapGenerator.Generate(this);
        foreach (var unit in units)
            unit.Initialize(this);
        foreach (var item in items)
            item.Initialize(this);
    }

    public Vector2Int WorldToCell(Vector3 worldPos) => (Vector2Int)tilemap.WorldToCell(worldPos);

    public Vector3 CellToCenterWorld(Vector2Int cellPos) =>
        tilemap.GetCellCenterWorld((Vector3Int)cellPos);

    public MapTile CellToTile(Vector2Int cellPos) => mapData.GetTile(cellPos.x, cellPos.y);

    public MapTile WorldToTile(Vector3 worldPos) => CellToTile(WorldToCell(worldPos));

    public bool CanPassThrough(MapTile tile, TeamData team)
    {
        switch (tile.TileData.TileCollisionOption)
        {
            case TileCollisionOption.Pass:
                return true;
            case TileCollisionOption.BlockFriendly:
                return tile.OwnedRegion != null && tile.OwnedRegion.OwnedTeam != team;
            case TileCollisionOption.BlockEnemy:
                return tile.OwnedRegion != null && OpponentTeam(tile.OwnedRegion.OwnedTeam) != team;
            default: // BlockAll
                return false;
        }
    }

    public TeamData OpponentTeam(TeamData team)
    {
        if (team == teamA)
            return teamB;
        if (team == teamB)
            return teamA;
        return null;
    }

    public TeamContext GetTeamContext(TeamData team) => teamContextTable.GetValueOrDefault(team, null);
}
