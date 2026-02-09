using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour, IMapInteractionContext
{
    [Header("References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TeamAreaViewer teamAreaViewer;

    private MapData mapData;
    public MapSpaceInfo MapSpaceInfo => mapData?.MapSpaceInfo;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
        teamAreaViewer?.ColorTiles(mapData);
    }

    public Vector3 CellToCenterWorld(Vector2Int cellPos) => tilemap.GetCellCenterWorld((Vector3Int)cellPos);

    public CollisionBound GetTileCollisionBound(Vector2Int cellPos)
    {
        var tile = GetTile(cellPos);
        return tile != null ? tile.TileData.CollisionBound 
                            : new CollisionBound { Type = CollisionBoundType.Square, Width = 1 };
    }

    public bool IsWalkable(Vector2Int cellPos, TeamData team)
    {
        MapTile tile = GetTile(cellPos);
        if (tile == null) return false;

        switch (tile.TileData.TileCollisionOption)
        {
            case TileCollisionOption.Pass:
                return true;
            case TileCollisionOption.BlockFriendly:
                return tile.OwnedRegion != null && tile.OwnedRegion.OwnedTeam != team;
            case TileCollisionOption.BlockEnemy:
                // TODO: Get Opponent
                return tile.OwnedRegion != null && !tile.OwnedRegion.OwnedTeam.IsOpponent(team);
            default: // BlockAll
                return false;
        }
    }

    public Vector2Int WorldToCell(Vector3 worldPos) => (Vector2Int)tilemap.WorldToCell(worldPos);

    public List<IMapObject> GetObjectsAt(Vector2Int cellPos) => GetTile(cellPos)?.MapObjects;

    // helpers

    public MapTile GetTile(Vector2Int cellPos) => mapData?.GetTile(cellPos.x, cellPos.y);

    public MapTile GetTileAtWorldPos(Vector3 worldPos) => GetTile(WorldToCell(worldPos));

    public MapTile GetRandomTile(Predicate<MapTile> filter)
    {
        int maxAttempts = 20;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            int x = UnityEngine.Random.Range(0, mapData.Width);
            int y = UnityEngine.Random.Range(0, mapData.Height);
            Vector2Int pos = new Vector2Int(x, y);

            MapTile tile = GetTile(pos);

            if (tile != null && filter(tile))
                return tile;
        }
        
        return null;
    }
}