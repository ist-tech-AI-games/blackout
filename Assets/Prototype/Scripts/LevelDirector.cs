using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public class LevelDirector : MonoBehaviour
{
    public event Action<MatchManager> OnLevelInitialized;

    [Header("Core Systems")]
    [SerializeField]
    private MatchManager matchManager;

    [SerializeField]
    private MapManager mapManager;

    [SerializeField]
    private UIManager uIManager;  // Logic에서 View를 참조하는 좋지 않은 방식이지만, 초기화 중앙화를 위한 설계.

    [Header("Generation")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [Header("Pooling Settings")]
    [SerializeField]
    private ItemObject itemPrefab;

    [SerializeField]
    private Transform itemParent;

    [SerializeField]
    private int initialPoolSize = 30;

    [SerializeField]
    private int maxPoolSize = 150;

    private GameEventBus eventBus;

    // Object Pool
    private IObjectPool<ItemObject> itemPool;
    private HashSet<ItemObject> activeItems = new();
    private bool isGamePlaying = false;

    public void Initialize(GameScenario gameScenario)
    {
        eventBus = gameScenario.EventBus;

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
            defaultCapacity: initialPoolSize,
            maxSize: maxPoolSize
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

        OnLevelInitialized?.Invoke(matchManager);
        uIManager.Initialize(matchManager);

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
}
