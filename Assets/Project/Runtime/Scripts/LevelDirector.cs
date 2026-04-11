using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

/// <summary>
/// Orchestrates level/episode lifecycle, map generation, and item spawning.
/// Manages object pooling for items, handles absorption intervals, and spawns special items dynamically.
/// Publishes episode started events for UI and other systems to initialize.
/// </summary>
public class LevelDirector : MonoBehaviour
{
    /// <summary>
    /// Invoked when a level is initialized and ready for play.
    /// </summary>
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

    /// <summary>All currently active (non-pooled) item objects, including carried items.</summary>
    public IReadOnlyCollection<ItemObject> ActiveItems => activeItems;
    private bool isGamePlaying = false;

    // Dynamic Spawning
    private ItemObject currentSpecialItem;
    private float currentSpawnTimer = 0f;

    /// <summary>
    /// Initializes the level director with game scenario settings.
    /// Configures map generator, creates item object pool, and subscribes to game events.
    /// </summary>
    /// <param name="gameScenario">Game scenario containing balance config and event bus.</param>
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

    /// <summary>
    /// Starts a new episode by performing reset.
    /// Called by GameScenario when starting an episode.
    /// </summary>
    public void StartEpisode()
    {
        PerformReset();
    }

    /// <summary>
    /// Ends the current episode.
    /// Stops gameplay updates (special item spawning).
    /// </summary>
    public void EndEpisode()
    {
        if (isGamePlaying)
            isGamePlaying = false;
    }

    /// <summary>
    /// Manual late update for frame-rate independent logic.
    /// Updates special item spawner when game is playing.
    /// </summary>
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
        mapManager.WireEventBusToRegions(eventBus);

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

    /// <summary>
    /// Handles absorption interval events by absorbing items in storage regions.
    /// Called periodically based on game timer.
    /// </summary>
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

    /// <summary>
    /// Spawns an item at the specified cell position.
    /// Called by MapGenerator during map initialization.
    /// </summary>
    /// <param name="data">Item data configuration.</param>
    /// <param name="amount">Initial stack amount.</param>
    /// <param name="cellPos">Cell position to spawn at.</param>
    public void SpawnItem(ItemData data, int amount, Vector2Int cellPos)
    {
        ItemObject item = itemPool.Get();

        item.RegisterToMap(data, amount, cellPos);
    }

    // === Pooling Callbacks ===

    /// <summary>
    /// Object pool callback for creating new item instances.
    /// </summary>
    /// <returns>Newly created and initialized item object.</returns>
    private ItemObject CreateItem()
    {
        ItemObject item = Instantiate(itemPrefab, itemParent);
        item.Initialize(matchManager, mapManager, itemPool, itemParent);
        return item;
    }

    /// <summary>
    /// Object pool callback when retrieving an item from the pool.
    /// Resets item state and adds to active items tracking.
    /// </summary>
    /// <param name="item">Item being retrieved from pool.</param>
    private void OnGetItem(ItemObject item)
    {
        item.ResetState();
        activeItems.Add(item);
    }

    /// <summary>
    /// Object pool callback when returning an item to the pool.
    /// Deactivates item and removes from active items tracking.
    /// </summary>
    /// <param name="item">Item being returned to pool.</param>
    private void OnReleaseItem(ItemObject item)
    {
        item.gameObject.SetActive(false);
        activeItems.Remove(item);
    }

    /// <summary>
    /// Object pool callback when destroying an item beyond pool capacity.
    /// </summary>
    /// <param name="item">Item to destroy.</param>
    private void OnDestroyItem(ItemObject item)
    {
        Destroy(item.gameObject);
    }

    // ===== Dynamic Spawning =====

    /// <summary>
    /// Updates special item spawner timer and spawns items on cooldown.
    /// Called from ManualLateUpdate when game is playing.
    /// </summary>
    /// <param name="dt">Delta time for this frame.</param>
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

    /// <summary>
    /// Filters tiles for special item spawning.
    /// Returns true if tile is in neutral region, matches spawn filter type, and has no items.
    /// </summary>
    /// <param name="mapTile">Tile to check.</param>
    /// <returns>True if tile is available for special item spawning.</returns>
    private bool FilterAvailableSpawnPos(MapTile mapTile) =>
        !matchManager.GetPlayableTeamContexts()
            .Contains(matchManager.GetTeamContext(mapTile.OwnedRegion.OwnedTeam))
        && mapTile.TileData == spawnItemTileFilter
        && !mapTile.MapObjects.Any(obj => obj is ItemObject);

    /// <summary>
    /// Spawns a random special item at a valid neutral location.
    /// Called when special item spawn timer reaches cooldown threshold.
    /// </summary>
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
