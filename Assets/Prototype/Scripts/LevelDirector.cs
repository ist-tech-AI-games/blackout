using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LevelDirector : MonoBehaviour
{
    [Header("Core Systems")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private MapManager mapManager;

    // 이거 abstract 단계로 옮길까..?
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

    // Object Pool
    private IObjectPool<ItemObject> itemPool;
    private List<ItemObject> activeItems = new List<ItemObject>();

    void Awake()
    {
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
    }

    void Start()
    {
        StartEpisode();
    }

    // === Episode Lifecycle ===

    public void StartEpisode()
    {
        MapData mapData = mapGenerator.GenerateMapData();
        mapManager.Initialize(mapData);
        mapGenerator.SpawnItemObjects(mapManager, SpawnItem);
        gameManager.ResetAllUnits(mapData);
    }

    public void EndEpisode()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            activeItems[i].OnDestroyed();
        }
        activeItems.Clear();

        StartEpisode();
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
        item.Initialize(gameManager, mapManager, itemPool, itemParent);
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
