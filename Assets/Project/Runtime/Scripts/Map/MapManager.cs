using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages map data, tile queries, and collision detection.
/// Provides interfaces for tile access, walkability checks, and coordinate conversions.
/// Uses pre-computed tile cache for efficient random tile selection.
/// </summary>
public class MapManager : MonoBehaviour, IMapInteractionContext
{
    [Header("References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TeamAreaViewer teamAreaViewer;

    private MapData mapData;

    /// <summary>
    /// Provides spawn points and region information for the current map.
    /// </summary>
    public MapSpaceInfo MapSpaceInfo => mapData?.MapSpaceInfo;

    /// <summary>Width of the current map in tiles.</summary>
    public int MapWidth => mapData?.Width ?? 0;

    /// <summary>Height of the current map in tiles.</summary>
    public int MapHeight => mapData?.Height ?? 0;

    /// <summary>
    /// World-space position of the bottom-left corner of the map.
    /// Used as origin for absolute position normalization in ML observations.
    /// </summary>
    public Vector2 MapOriginWorld => mapData != null
        ? (Vector2)tilemap.CellToWorld((Vector3Int)mapData.MapSpaceInfo.BottomLeft)
        : Vector2.zero;

    // Pre-computed tile cache for efficient random sampling
    private List<MapTile> allTilesCache;

    /// <summary>
    /// Initializes map manager with map data and builds tile cache.
    /// </summary>
    /// <param name="mapData">Map data containing tile information and regions.</param>
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

    /// <summary>
    /// Converts cell coordinates to world position (center of tile).
    /// </summary>
    /// <param name="cellPos">Cell position in grid coordinates.</param>
    /// <returns>World position at the center of the tile.</returns>
    public Vector3 CellToCenterWorld(Vector2Int cellPos) => tilemap.GetCellCenterWorld((Vector3Int)cellPos);

    /// <summary>
    /// Gets the collision bound for a tile at the given position.
    /// Returns default square bound if tile doesn't exist.
    /// </summary>
    /// <param name="cellPos">Cell position to query.</param>
    /// <returns>Collision bound of the tile.</returns>
    public CollisionBound GetTileCollisionBound(Vector2Int cellPos)
    {
        var tile = GetTile(cellPos);
        return tile != null ? tile.TileData.CollisionBound 
                            : new CollisionBound { Type = CollisionBoundType.Square, Width = 1 };
    }

    /// <summary>
    /// Checks if a tile is walkable for the given team.
    /// Considers tile collision options (Pass, BlockFriendly, BlockEnemy, BlockAll).
    /// </summary>
    /// <param name="cellPos">Cell position to check.</param>
    /// <param name="team">Team requesting walkability check.</param>
    /// <returns>True if the team can walk on this tile.</returns>
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

    /// <summary>
    /// Converts world position to cell coordinates.
    /// </summary>
    /// <param name="worldPos">World position to convert.</param>
    /// <returns>Cell position in grid coordinates.</returns>
    public Vector2Int WorldToCell(Vector3 worldPos) => (Vector2Int)tilemap.WorldToCell(worldPos);

    /// <summary>
    /// Gets all map objects at the given cell position.
    /// </summary>
    /// <param name="cellPos">Cell position to query.</param>
    /// <returns>List of map objects at the position, or null if tile doesn't exist.</returns>
    public List<IMapObject> GetObjectsAt(Vector2Int cellPos) => GetTile(cellPos)?.MapObjects;

    /// <summary>
    /// Sets the GameEventBus on all map regions so they can publish gameplay events.
    /// Must be called after Initialize().
    /// </summary>
    public void WireEventBusToRegions(GameEventBus eventBus)
    {
        foreach (var tile in allTilesCache)
            if (tile.OwnedRegion != null)
                tile.OwnedRegion.EventBus = eventBus;
    }

    // helpers

    /// <summary>
    /// Gets the tile at the given cell position.
    /// </summary>
    /// <param name="cellPos">Cell position to query.</param>
    /// <returns>Map tile at the position, or null if out of bounds.</returns>
    public MapTile GetTile(Vector2Int cellPos)
    {
        Vector2Int bottomLeft = mapData.MapSpaceInfo.BottomLeft;
        return mapData?.GetTile(cellPos.x - bottomLeft.x, cellPos.y - bottomLeft.y);
    }

    /// <summary>
    /// Gets the tile at the given world position.
    /// </summary>
    /// <param name="worldPos">World position to query.</param>
    /// <returns>Map tile at the position, or null if out of bounds.</returns>
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