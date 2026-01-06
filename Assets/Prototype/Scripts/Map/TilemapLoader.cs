using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// MapGenerator의 구현체 중 하나로서, 미리 그려진 타일맵을 파싱하는 버전.
public class TilemapLoader : MapGenerator
{
    [SerializeField]
    private Tilemap levelMap;

    [SerializeField]
    private RectInt mapArea;

    [SerializeField]
    private MapTileData[] mapTileData;

    [SerializeField]
    private MapTileData defaultTile;

    [Serializable]
    public class RegionData
    {
        public string Name;
        public TeamData Team;
        public RectInt[] Area;
    }

    // 앞쪽 물체 우선 적용
    [SerializeField]
    private RegionData[] regions;

    [SerializeField]
    private RegionData defaultRegion;

    public override MapData Generate(GameManager gameManager)
    {
        Dictionary<TileBase, MapTileData> tileDataMap = new();
        foreach (var tileData in mapTileData)
            tileDataMap.Add(tileData.TileData, tileData);

        MapTile[,] mapTiles = new MapTile[mapArea.height, mapArea.width];
        List<MapRegion> mapRegions = new(16);
        foreach (var regionData in regions)
        {
            MapRegion mapRegion = new MapRegion();
            mapRegion.OwnedTeam = regionData.Team;
            mapRegions.Add(mapRegion);

            foreach (var area in regionData.Area)
                for (int y = area.yMin; y < area.yMax; y++)
                {
                    int i = y - area.yMin;
                    for (int x = area.xMin; x < area.xMax; x++)
                        if (mapArea.Contains(new(x, y)))
                        {
                            int j = x - area.xMin;
                            if (mapTiles[i, j] == null)
                            {
                                mapTiles[i, j] = new MapTile(
                                    tileDataMap.GetValueOrDefault(
                                        levelMap.GetTile(new(x, y, 0)),
                                        defaultTile
                                    ),
                                    mapRegion, new(x, y)
                                );
                                mapRegion.MapTiles.Add(mapTiles[i, j]);
                            }
                        }
                }
        }

        // fill defaults
        MapRegion defaultMapRegion = new();
        defaultMapRegion.OwnedTeam = defaultRegion.Team;
        mapRegions.Add(defaultMapRegion);
        for (int i = 0; i < mapArea.height; i++)
        for (int j = 0; j < mapArea.width; j++)
            if (mapTiles[i, j] == null)
            {
                Vector2Int pos = new(j + mapArea.xMin, i + mapArea.yMin);
                mapTiles[i, j] = new MapTile(
                    tileDataMap.GetValueOrDefault(
                        levelMap.GetTile((Vector3Int)pos),
                        defaultTile
                    ),
                    defaultMapRegion, pos
                );
                defaultMapRegion.MapTiles.Add(mapTiles[i, j]);
            }

        return new MapData(mapTiles, mapRegions.ToArray(), levelMap, new(mapArea.xMin, mapArea.yMax));
    }
}
