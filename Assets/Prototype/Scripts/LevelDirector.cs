using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public class LevelDirector : MonoBehaviour
{
    [Header("Core Systems")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private MapManager mapManager;

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
    private HashSet<ItemObject> activeItems = new();
    private bool restartPending = false;

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

    private void LateUpdate()
    {
        if (restartPending)
        {
            PerformReset();
            restartPending = false;
        }
    }

    // === Episode Lifecycle ===

    public void StartEpisode()
    {
        PerformReset();
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
        Debug.Log($"Starting episode; pool: {itemPool.CountInactive}, active: {activeItems.Count}");
        mapGenerator.SpawnItemObjects(mapManager, SpawnItem);
        Debug.Log($"Item generated; pool: {itemPool.CountInactive}, active: {activeItems.Count}");

        gameManager.ResetAllUnits(mapData);
        gameManager.GetTeamContext(gameManager.NeutralTeam).OnScoreChanged += CheckGameEnd;
    }

    public void EndEpisode()
    {
        var neutralContext = gameManager.GetTeamContext(gameManager.NeutralTeam);
        neutralContext.OnScoreChanged -= CheckGameEnd;

        restartPending = true;
    }

    // === Event Handlers ===
    private void CheckGameEnd(int score)
    {
        if (score <= 0)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        TeamData winner;

        int scoreA = gameManager.GetTeamContext(gameManager.TeamA).Score;
        int scoreB = gameManager.GetTeamContext(gameManager.TeamB).Score;

        if (scoreA > scoreB)
            winner = gameManager.TeamA;
        else if (scoreA < scoreB)
            winner = gameManager.TeamB;
        else
            winner = null;

        // TODO
        if (winner == null)
            Debug.Log("Draw!");
        else
            Debug.Log($"{winner.TeamName} win!");

        EndEpisode();
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
