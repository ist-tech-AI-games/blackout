using System;
using UnityEngine;
using UnityEngine.Pool;

public class ItemObject : MonoBehaviour, IMapObject, IResettable
{
    public event Action<ItemData, int> OnDataUpdated;

    public enum ItemState
    {
        OnGround,
        Carried,
        InPool
    }

    [Header("Data")]
    [field: SerializeField] public ItemData ItemData { get; private set; }
    [field: SerializeField] public int ItemAmount { get; private set; } = 1;

    // Properties
    public CollisionBound CollisionBound => ItemData != null ? ItemData.CollisionBound : default;
    public Vector2 GlobalPos => transform.position;
    public MapTile OwnedTile { get; private set; }
    public ItemState State { get; private set; } = ItemState.InPool;

    // Internal State
    private TeamContext currentAppliedContext;
    private MatchManager matchManager;
    private MapManager mapManager;
    private IObjectPool<ItemObject> managedPool;
    private Transform worldParent;

    // ===== Initialization & Lifecycle =====

    public void Initialize(MatchManager matchManager, MapManager mapManager, IObjectPool<ItemObject> pool, Transform worldParent)
    {
        this.matchManager = matchManager;
        this.mapManager = mapManager;
        managedPool = pool;
        this.worldParent = worldParent;
    }

    public void ResetState()
    {
        State = ItemState.OnGround;
        currentAppliedContext = null;
        OwnedTile = null;
        gameObject.SetActive(true);
    }

    // 맵 생성 시 최초 배치
    public void RegisterToMap(ItemData itemData, int amount, Vector2Int cellPos)
    {
        ItemData = itemData;
        ItemAmount = amount;

        MapTile tile = mapManager.GetTile(cellPos);
        if (tile != null)
        {
            MoveToTile(tile);
        }
        else
        {
            Debug.LogError($"Invalid tile position for item: {cellPos}");
            OnDestroyed();
        }

        OnDataUpdated?.Invoke(ItemData, ItemAmount);
    }

    // ===== State Machine =====

    public void OnPickedUp(Unit unit)
    {
        if (State == ItemState.Carried) return;

        matchManager.EventBus.Unit.PublishItemPickedUp(unit, this);

        ExitCurrentRegion(ItemExitReason.Dropped); 
        RemoveFromMap();

        State = ItemState.Carried;
    }

    public void OnDropped(MapTile targetTile)
    {
        State = ItemState.OnGround;
        MoveToTile(targetTile);
    }

    public void OnAbsorbed()
    {
        ExitCurrentRegion(ItemExitReason.Absorbed);
        OnDestroyed(skipEffect: true);
    }

    public void OnDestroyed(bool skipEffect = false)
    {
        if (!gameObject.activeSelf || State == ItemState.InPool)
            return;
        if (!skipEffect)
            ExitCurrentRegion(ItemExitReason.Destroyed);

        RemoveFromMap();
        
        State = ItemState.InPool;

        if (managedPool != null) managedPool.Release(this);
        else Destroy(gameObject);
    }

    // ===== Logic & Data Modification =====

    public void UpdateAmount(int newAmount)
    {
        if (newAmount == ItemAmount) return;

        // 수량 변경 시: [Refresh 퇴장] -> [데이터 변경] -> [재진입]
        if (currentAppliedContext != null)
            ApplyEffectInternal(currentAppliedContext, ItemAmount, ItemExitReason.Refresh, isEnter: false);

        ItemAmount = newAmount;
        OnDataUpdated?.Invoke(ItemData, newAmount);

        if (currentAppliedContext != null)
            ApplyEffectInternal(currentAppliedContext, ItemAmount, ItemExitReason.Refresh, isEnter: true);
    }

    // ===== Map & Effect Logic Helpers =====

    private void MoveToTile(MapTile newTile)
    {
        if (newTile == null) return;

        transform.SetParent(worldParent);
        transform.position = mapManager.CellToCenterWorld(newTile.CellPos);

        RemoveFromMap();
        AddToMap(newTile);

        UpdateRegionState(newTile.OwnedRegion);
    }

    private void AddToMap(MapTile tile)
    {
        OwnedTile = tile;
        tile.MapObjects.Add(this);
    }

    private void RemoveFromMap()
    {
        if (OwnedTile != null)
        {
            OwnedTile.MapObjects.Remove(this);
            OwnedTile = null;
        }
    }

    // 영역 변경에 따른 효과 상태 갱신
    private void UpdateRegionState(MapRegion region)
    {
        if (region == null) return;

        TeamData ownerTeam = region.OwnedTeam;
        TeamContext newContext = (ownerTeam != null) ? matchManager.GetTeamContext(ownerTeam) : null;

        // 구역 전이
        if (currentAppliedContext != newContext)
        {
            ExitCurrentRegion(ItemExitReason.Dropped);

            if (newContext != null)
            {
                ApplyEffectInternal(newContext, ItemAmount, ItemExitReason.Dropped, isEnter: true);
                currentAppliedContext = newContext;
            }
        }
    }

    private void ExitCurrentRegion(ItemExitReason reason)
    {
        if (currentAppliedContext != null)
        {
            ApplyEffectInternal(currentAppliedContext, ItemAmount, reason, isEnter: false);
            currentAppliedContext = null;
        }
    }

    // 실제 Effect SO 호출 (가장 낮은 레벨의 로직)
    private void ApplyEffectInternal(TeamContext context, int amount, ItemExitReason reason, bool isEnter)
    {
        if (ItemData == null || ItemData.Effect == null || context == null) return;

        if (isEnter)
            ItemData.Effect.EnterEffect(context, amount);
        else
            ItemData.Effect.ExitEffect(context, amount, reason);
    }

    public bool IsInteractable(TeamData team)
    {
        if (ItemData == null) return false;
        
        // 현재 타일의 소유권 확인
        TeamData regionTeam = OwnedTile?.OwnedRegion?.OwnedTeam;

        switch (ItemData.InteractionOption)
        {
            case ObjectInteractionOption.All:
                return true;
            case ObjectInteractionOption.IgnoreFriend:
                return regionTeam != team; // 내 땅이 아니면 상호작용 가능
            case ObjectInteractionOption.IgnoreEnemy:
                return regionTeam == null || regionTeam != matchManager.OpponentTeam(team); // 적 땅만 아니면 됨
            default:
                return false;
        }
    }

    // ===== Interface Implementations =====

    public void OnOverlapped(IMapObject other) { /* No interaction logic needed here yet */ }
}