using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class ProceduralMapGenerator : MapGenerator
{
    [Header("Base Map Settings")]
    [SerializeField]
    private Tilemap levelMap;

    [SerializeField]
    private RectInt mapArea = new(0, 0, 24, 24);

    [SerializeField]
    private MapTileData[] mapTileData;

    [SerializeField]
    private MapTileData defaultTileData;

    [SerializeField]
    private MapTileData storageTileData;

    [SerializeField]
    private TeamData teamA;

    [SerializeField]
    private TeamData teamB;

    [Header("Spawn Points")]
    [SerializeField]
    private Vector2Int teamASpawnPoint;

    [SerializeField]
    private Vector2Int teamBSpawnPoint;

    [Header("Fixed Regions")]
    [SerializeField]
    private RegionData[] fixedRegions;

    [SerializeField]
    private RegionData defaultRegion;

    [Header("Procedural Storages")]
    [Tooltip("A 팀 데이터만. 상대 팀은 자동으로 대칭 생성.")]
    [SerializeField]
    private List<RegionData> storageCandidatesA;

    [Header("Item Spawning")]
    [SerializeField]
    private MapTileData itemTileFilter;

    [SerializeField]
    private ItemData[] itemTypes;

    private Dictionary<TileBase, MapTileData> tileDataLookup;

    public override MapData GenerateMapData()
    {
        tileDataLookup = new Dictionary<TileBase, MapTileData>();
        foreach (var data in mapTileData)
        {
            if (data != null && data.TileData != null)
                tileDataLookup.TryAdd(data.TileData, data);
        }

        MapTile[,] mapTiles = new MapTile[mapArea.height, mapArea.width];
        List<MapRegion> mapRegions = new List<MapRegion>();

        foreach (var regionData in fixedRegions)
            CreateAndApplyRegion(regionData, mapRegions, mapTiles);

        GenerateProceduralWarehouses(mapRegions, mapTiles);

        FillDefaultRegion(mapRegions, mapTiles);

        MapSpaceInfo spaceInfo = new MapSpaceInfo(
            levelMap,
            mapArea.min,
            teamASpawnPoint,
            teamBSpawnPoint
        );
        return new MapData(mapTiles, mapRegions.ToArray(), spaceInfo);
    }

    public override void SpawnItemObjects(MapManager mapManager, SpawnItemCallback spawnItemCallback)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        for (int i = 0; i < mapArea.height; i++)
        {
            for (int j = 0; j < mapArea.width; j++)
            {
                MapTile mapTile = mapManager.GetTile(new(j, i));
                if (
                    mapTile.OwnedRegion.OwnedTeam != teamA
                    && mapTile.OwnedRegion.OwnedTeam != teamB
                    && mapTile.TileData == itemTileFilter
                )
                {
                    validPositions.Add(new Vector2Int(j, i));
                }
            }
        }

        // Spawn one of each non-battery item type (index 1+), symmetric if configured
        for (int typeIdx = 1; typeIdx < itemTypes.Length && validPositions.Count > 0; typeIdx++)
        {
            int idx = Random.Range(0, validPositions.Count);
            Vector2Int posA = validPositions[idx];
            validPositions.RemoveAt(idx);
            spawnItemCallback(itemTypes[typeIdx], 1, posA);

            if (BalanceConfig.SymmetricBatterySpawn)
            {
                Vector2Int posB = new(posA.y, posA.x);
                if (posA != posB && validPositions.Contains(posB))
                {
                    spawnItemCallback(itemTypes[typeIdx], 1, posB);
                    validPositions.Remove(posB);
                }
            }
        }

        // Fill remaining score with Battery (index 0)
        int currentScore = 0;

        while (currentScore < BalanceConfig.InitialBatteryTotalScore && validPositions.Count > 0)
        {
            int idx = Random.Range(0, validPositions.Count);
            Vector2Int posA = validPositions[idx];
            validPositions.RemoveAt(idx);

            int amount = Random.Range(BalanceConfig.MinBatteryAmount, BalanceConfig.MaxBatteryAmount + 1);
            ItemData data = itemTypes[0];

            spawnItemCallback(data, amount, posA);
            currentScore += amount;

            if (BalanceConfig.SymmetricBatterySpawn)
            {
                Vector2Int posB = new(posA.y, posA.x);

                if (posA != posB && validPositions.Contains(posB))
                {
                    spawnItemCallback(data, amount, posB);
                    currentScore += amount;
                    validPositions.Remove(posB);
                }
            }
        }
    }

    private void GenerateProceduralWarehouses(List<MapRegion> regions, MapTile[,] mapTiles)
    {
        var shuffled = storageCandidatesA.OrderBy(x => Random.value).ToList();
        int count = Mathf.Min(BalanceConfig.StorageCountPerTeam, shuffled.Count);

        for (int i = 0; i < count; i++)
        {
            // Team A
            var dataA = shuffled[i];
            dataA.Team = teamA;
            CreateAndApplyRegion(dataA, regions, mapTiles, teamASpawnPoint, isProcedural: true);

            // Team B
            var dataB = RegionUtils.CreateSymmetricRegion(dataA, teamB);
            CreateAndApplyRegion(dataB, regions, mapTiles, teamBSpawnPoint, isProcedural: true);
        }
    }

    // 영역을 생성하고 해당 영역의 타일 데이터를 맵 배열에 할당
    private void CreateAndApplyRegion(
        RegionData data,
        List<MapRegion> regions,
        MapTile[,] mapTiles,
        Vector2Int? anchorPos = null,
        bool isProcedural = false
    )
    {
        MapRegion region = data.CreateRuntimeRegion();
        regions.Add(region);

        foreach (RectInt area in data.Area)
        {
            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!mapArea.Contains(pos))
                        continue;

                    int i = y - mapArea.yMin;
                    int j = x - mapArea.xMin;

                    if (mapTiles[i, j] != null)
                        continue;

                    MapTileData tileDataToUse = isProcedural
                        ? storageTileData
                        : GetTileDataFromMap(pos);

                    mapTiles[i, j] = new MapTile(tileDataToUse, region, pos);

                    region.MapTiles.Add(mapTiles[i, j]);
                }
            }
        }

        if (region is Storage storage && anchorPos.HasValue)
        {
            storage.Initialize(anchorPos.Value);
        }
    }

    private void FillDefaultRegion(List<MapRegion> regions, MapTile[,] mapTiles)
    {
        MapRegion defaultMapRegion = defaultRegion.CreateRuntimeRegion();
        regions.Add(defaultMapRegion);

        for (int i = 0; i < mapArea.height; i++)
        {
            for (int j = 0; j < mapArea.width; j++)
            {
                if (mapTiles[i, j] == null)
                {
                    Vector2Int pos = new Vector2Int(j + mapArea.xMin, i + mapArea.yMin);
                    MapTileData data = GetTileDataFromMap(pos);

                    mapTiles[i, j] = new MapTile(data, defaultMapRegion, pos);
                    defaultMapRegion.MapTiles.Add(mapTiles[i, j]);
                }
            }
        }
    }

    private MapTileData GetTileDataFromMap(Vector2Int pos)
    {
        TileBase tileBase = levelMap.GetTile((Vector3Int)pos);
        return tileDataLookup.GetValueOrDefault(tileBase, defaultTileData);
    }
}
