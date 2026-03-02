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

    // Pre-computed tile cache for efficient random sampling
    private List<MapTile> allTilesCache;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
        teamAreaViewer?.ColorTiles(mapData);

        // Pre-compute all tiles for faster access
        BuildTileCache();
    }

    /// <summary>
    /// Builds cache of all tiles for efficient random access.
    /// Called once during initialization since map doesn't change at runtime.
    /// </summary>
    private void BuildTileCache()
    {
        allTilesCache = new List<MapTile>(mapData.Width * mapData.Height);

        for (int y = 0; y < mapData.Height; y++)
        {
            for (int x = 0; x < mapData.Width; x++)
            {
                MapTile tile = mapData.GetTile(x, y);
                if (tile != null)
                {
                    allTilesCache.Add(tile);
                }
            }
        }
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

    /// <summary>
    /// Gets a random tile that matches the filter predicate.
    /// Uses pre-computed tile cache for reliable selection - guaranteed to find a tile if one exists.
    /// Much more reliable than rejection sampling (old approach could fail after 20 attempts).
    /// </summary>
    public MapTile GetRandomTile(Predicate<MapTile> filter)
    {
        if (allTilesCache == null || allTilesCache.Count == 0)
            return null;

        // Build list of matching tiles (one-time O(n) scan instead of random sampling)
        List<MapTile> candidates = new List<MapTile>();
        foreach (MapTile tile in allTilesCache)
        {
            if (filter(tile))
            {
                candidates.Add(tile);
            }
        }

        // Return random tile from candidates
        if (candidates.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }
}