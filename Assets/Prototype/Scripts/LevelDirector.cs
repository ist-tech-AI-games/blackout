using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class LevelDirector : MonoBehaviour
{
    public event Action<MatchManager> OnLevelInitialized;

    [Header("Core Systems")]
    [SerializeField]
    private MatchManager matchManager;

    [SerializeField]
    private MapManager mapManager;

    [Header("Generation")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [Header("Pooling Settings")]
    [SerializeField]
    private ItemObject itemPrefab;

    [SerializeField]
    private Transform itemParent;

    [Header("Dynamic Spawning")]
    [SerializeField]
    private List<ItemData> specialItemPrototypes;

    [SerializeField]
    private MapTileData spawnItemTileFilter;

    private GameEventBus eventBus;
    private GameScenario gameScenario;

    // Object Pool
    private IObjectPool<ItemObject> itemPool;
    private HashSet<ItemObject> activeItems = new();
    private bool isGamePlaying = false;

    // Dynamic Spawning
    private ItemObject currentSpecialItem;
    private float currentSpawnTimer = 0f;

    public void Initialize(GameScenario gameScenario)
    {
        this.gameScenario = gameScenario;
        eventBus = gameScenario.EventBus;

        // Configure map generator with balance settings
        mapGenerator.SetConfig(gameScenario.BalanceConfig);

        itemPool = new ObjectPool<ItemObject>(
            createFunc: CreateItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
#if UNITY_EDITOR
            collectionCheck: true,
#else
            collectionCheck: false,
#endif
            defaultCapacity: gameScenario.BalanceConfig.ItemPoolInitialSize,
            maxSize: gameScenario.BalanceConfig.ItemPoolMaxSize
        );

        eventBus.Flow.OnGameEnded += (winner) => EndEpisode();
        eventBus.World.OnAbsorptionInterval += OnAbsorption;
    }

    // === Episode Lifecycle ===
    public void StartEpisode()
    {
        PerformReset();
    }

    public void EndEpisode()
    {
        if (isGamePlaying)
            isGamePlaying = false;
    }

    public void ManualLateUpdate()
    {
        if (!isGamePlaying) return;

        UpdateSpecialItemSpawner(Time.deltaTime);
    }

    private void PerformReset()
    {
        var activeList = activeItems.ToList();
        for (int i = activeList.Count - 1; i >= 0; i--)
        {
            activeList[i].OnDestroyed();
        }
        activeItems.Clear();

        MapData mapData = mapGenerator.GenerateMapData();
        mapManager.Initialize(mapData);

        matchManager.ResetAllUnits(mapData);
        mapGenerator.SpawnItemObjects(mapManager, SpawnItem);

        currentSpecialItem = null;
        currentSpawnTimer = 0f;

        // Publish episode started event for UI and other systems to initialize
        OnLevelInitialized?.Invoke(matchManager);
        eventBus.Flow.PublishEpisodeStarted(matchManager, gameScenario);

        isGamePlaying = true;
    }

    // === Event Handlers ===

    private void OnAbsorption()
    {
        Debug.Log("Absorption(is Game playing?)");
        if (!isGamePlaying) return;
        Debug.Log("Absorption");

        var snapshot = new List<ItemObject>(activeItems);

        foreach (var item in snapshot)
        {
            if (item.OwnedTile != null && item.OwnedTile.OwnedRegion is Storage)
            {
                item.OnAbsorbed();
            }
        }
    }

    // === Item Spawning (Public Interface) ===

    // Generator가 호출
    public void SpawnItem(ItemData data, int amount, Vector2Int cellPos)
    {
        ItemObject item = itemPool.Get();

        item.RegisterToMap(data, amount, cellPos);
    }

    // === Pooling Callbacks ===

    private ItemObject CreateItem()
    {
        ItemObject item = Instantiate(itemPrefab, itemParent);
        item.Initialize(matchManager, mapManager, itemPool, itemParent);
        return item;
    }

    private void OnGetItem(ItemObject item)
    {
        item.ResetState();
        activeItems.Add(item);
    }

    private void OnReleaseItem(ItemObject item)
    {
        item.gameObject.SetActive(false);
        activeItems.Remove(item);
    }

    private void OnDestroyItem(ItemObject item)
    {
        Destroy(item.gameObject);
    }

    // ===== Dynamic Spawning =====

    private void UpdateSpecialItemSpawner(float dt)
    {
        if (currentSpecialItem != null && currentSpecialItem.gameObject.activeSelf)
            return;

        currentSpawnTimer += dt;

        if (currentSpawnTimer >= gameScenario.BalanceConfig.SpecialItemSpawnCooldown)
        {
            SpawnSpecialItem();
            currentSpawnTimer = 0f;
        }
    }

    // 지정된 유형의 공역 타일이며 다른 아이템이 없을 것.
    private bool FilterAvailableSpawnPos(MapTile mapTile) => 
        !matchManager.GetPlayableTeamContexts()
            .Contains(matchManager.GetTeamContext(mapTile.OwnedRegion.OwnedTeam)) 
        && mapTile.TileData == spawnItemTileFilter
        && !mapTile.MapObjects.Any(obj => obj is ItemObject);

    private void SpawnSpecialItem()
    {
        if (specialItemPrototypes == null || specialItemPrototypes.Count == 0) return;

        ItemData selectedData = specialItemPrototypes[Random.Range(0, specialItemPrototypes.Count)];

        MapTile targetTile = mapManager.GetRandomTile(FilterAvailableSpawnPos);
        if (targetTile == null)
        {
            currentSpawnTimer = gameScenario.BalanceConfig.SpecialItemSpawnCooldown - 1f; // 재시도
            return;
        }

        ItemObject newItem = itemPool.Get();
        newItem.RegisterToMap(selectedData, 1, targetTile.CellPos);

        currentSpecialItem = newItem;

        Debug.Log($"Special Item Spawned: {selectedData.name} at {targetTile.CellPos}");
    }
}
