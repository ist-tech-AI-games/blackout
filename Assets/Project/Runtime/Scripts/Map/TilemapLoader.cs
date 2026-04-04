using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// MapGenerator의 구현체 중 하나로서, 미리 그려진 타일맵을 파싱하는 버전.
public class TilemapLoader : MapGenerator
{
    [Header("Map Source")]
    [SerializeField]
    private Tilemap levelMap;

    [SerializeField]
    private RectInt mapArea;

    [Header("Tile Settings")]
    [SerializeField]
    private MapTileData[] mapTileData;

    [SerializeField]
    private MapTileData defaultTile;

    [Header("Region Data")]
    // 앞쪽 물체 우선 적용
    [SerializeField]
    private RegionData[] regions;

    [SerializeField]
    private RegionData defaultRegion;

    [Header("Spawn Settings")]
    [SerializeField]
    private Vector2Int teamASpawnPoint;

    [SerializeField]
    private Vector2Int teamBSpawnPoint;

    public override MapData GenerateMapData()
    {
        var tileDataLookup = CreateTileDataLookup();

        MapTile[,] mapTiles = new MapTile[mapArea.height, mapArea.width];
        List<MapRegion> mapRegions = new(16);

        foreach (var regionData in regions)
        {
            MapRegion mapRegion = regionData.CreateRuntimeRegion();
            mapRegions.Add(mapRegion);

            FillMapTiles(tileDataLookup, mapTiles, regionData, mapRegion);
        }

        FillDefaultTiles(tileDataLookup, mapTiles, mapRegions);

        MapSpaceInfo mapSpaceInfo = new(
            levelMap,
            new(mapArea.xMin, mapArea.yMin),
            teamASpawnPoint,
            teamBSpawnPoint
        );

        return new MapData(mapTiles, mapRegions.ToArray(), mapSpaceInfo);
    }

    public override void SpawnItemObjects(MapManager mapManager, SpawnItemCallback spawnItemCallback)
    {
        return;
    }

    private void FillDefaultTiles(
        Dictionary<TileBase, MapTileData> tileDataLookup,
        MapTile[,] mapTiles,
        List<MapRegion> regions
    )
    {
        MapRegion defaultMapRegion = defaultRegion.CreateRuntimeRegion();
        regions.Add(defaultMapRegion);

        for (int i = 0; i < mapArea.height; i++)
        for (int j = 0; j < mapArea.width; j++)
            if (mapTiles[i, j] == null)
            {
                int x = j + mapArea.xMin;
                int y = i + mapArea.yMin;
                CreateAndRegisterTile(tileDataLookup, mapTiles, defaultMapRegion, x, y, i, j);
            }
    }

    private void FillMapTiles(
        Dictionary<TileBase, MapTileData> tileDataLookup,
        MapTile[,] mapTiles,
        RegionData regionData,
        MapRegion mapRegion
    )
    {
        if (regionData.Area == null)
            return;

        foreach (var area in regionData.Area)
            for (int y = area.yMin; y < area.yMax; y++)
            {
                int i = y - mapArea.yMin;
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    Vector2Int pos = new(x, y);
                    if (!mapArea.Contains(pos))
                        continue;

                    int j = x - mapArea.xMin;
                    if (mapTiles[i, j] != null)
                        continue;

                    CreateAndRegisterTile(tileDataLookup, mapTiles, mapRegion, x, y, i, j);
                }
            }
    }

    private void CreateAndRegisterTile(
        Dictionary<TileBase, MapTileData> tileDataLookup,
        MapTile[,] mapTiles,
        MapRegion region,
        int x,
        int y,
        int i,
        int j
    )
    {
        mapTiles[i, j] = new MapTile(
            tileDataLookup.GetValueOrDefault(levelMap.GetTile(new(x, y, 0)), defaultTile),
            region,
            new(x, y)
        );
        region.MapTiles.Add(mapTiles[i, j]);
    }

    private Dictionary<TileBase, MapTileData> CreateTileDataLookup()
    {
        Dictionary<TileBase, MapTileData> tileDataMap = new();
        if (mapTileData == null)
            return tileDataMap;

        foreach (var tileData in mapTileData)
            tileDataMap.Add(tileData.TileData, tileData);
        return tileDataMap;
    }
}
