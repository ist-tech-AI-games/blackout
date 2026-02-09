using System;
using UnityEngine;

public class Unit : MonoBehaviour, IMapObject, IResettable
{
    // Events
    public event Action<UnitData> OnClassChanged;
    public event Action<Unit> OnUnitDead;

    [field: SerializeField]
    public UnitData UnitData { get; private set; }

    [field: SerializeField]
    public TeamData Team { get; private set; }

    public CollisionBound CollisionBound => UnitData.CollisionBound;
    public Vector2 GlobalPos => transform.position;

    public MapTile OwnedTile { get; set; }

    [SerializeField]
    private Transform itemHolder;

    // dependencies
    private MatchManager matchManager;
    private MapManager mapManager;
    private UnitMovementSystem unitMovementSystem;
    private UnitInteractionSystem unitInteractionSystem;

    public ItemObject HoldingItem { get; private set; } = null;
    public float MoveSpeed
    {
        get
        {
            // 주의: 초기화 이후에 쓰지 않으면 적용 안 됨.
            if (matchManager == null) return UnitData.BaseSpeed;

            var context = matchManager.GetTeamContext(this.Team);
            if (context == null) return UnitData.BaseSpeed;

            return context.CalculateStat(this, StatType.MoveSpeed, UnitData.BaseSpeed);
        }
    }

    public void Initialize(MatchManager matchManager, MapManager mapManager)
    {
        this.matchManager = matchManager;
        this.mapManager = mapManager;
        unitMovementSystem = new(mapManager, Team);
        unitInteractionSystem = new(mapManager, this);
    }

    public void ResetState()
    {
        if (HoldingItem != null)
        {
            HoldingItem.OnDestroyed();
            HoldingItem = null;
        }

        gameObject.SetActive(true);
        OwnedTile = null;
    }

    /// <summary>
    /// 성소가 요청. 조건이 맞으면 변신.
    /// </summary>
    /// <returns>변신 성공 여부</returns>
    public bool TryTransform(UnitData requiredInput, UnitData targetOutput)
    {
        if (UnitData != requiredInput)
            return false;

        SetUnitClass(targetOutput);
        return true;
    }

    /// <summary>
    /// 창고가 요청. 들고 있는 아이템 가져 옴 (이전. 대여 아님.)
    /// </summary>
    public ItemObject RetrieveItem()
    {
        if (HoldingItem == null)
            return null;

        ItemObject item = HoldingItem;
        HoldingItem = null;

        // (선택사항) 아이템 부모 관계 해제 등은 ItemObject.OnDropped에서 처리됨
        return item;
    }

    public void SetUnitClass(UnitData unitData)
    {
        UnitData = unitData;
        if (!unitData.Collectable && HoldingItem != null)
        {
            HoldingItem.OnDestroyed();
            HoldingItem = null;
        }
        OnClassChanged?.Invoke(unitData);
    }

    public void Move(Vector2 input, float deltaTime)
    {
        Vector2 targetPosition = unitMovementSystem.CalculateNextPosition(
            transform.position,
            input.normalized * deltaTime * MoveSpeed,
            UnitData.CollisionBound
        );
        ChangePos(targetPosition);
        unitInteractionSystem.ProcessInteractions(targetPosition);
    }

    // NOTE: This does not check for the tile collision, so DON'T teleport to blocking tile.
    public void Teleport(Vector2Int pos)
    {
        Vector2 worldPos = mapManager.CellToCenterWorld(pos);
        ChangePos(worldPos);
        unitInteractionSystem.ProcessInteractions(worldPos);
    }

    private void ChangePos(Vector2 targetPosition)
    {
        MapTile currentTile = mapManager.GetTileAtWorldPos(targetPosition);

        if (currentTile != null && OwnedTile != currentTile)
        {
            OwnedTile?.OnObjectExit(this);
            currentTile.OnObjectEnter(this);

            MapRegion prevRegion = OwnedTile?.OwnedRegion;
            OwnedTile = currentTile;

            if (prevRegion != OwnedTile.OwnedRegion)
            {
                prevRegion?.OnUnitExit(this);
                OwnedTile.OwnedRegion?.OnUnitEnter(this);
            }
        }

        transform.position = targetPosition;
    }

    public void OnOverlapped(IMapObject other)
    {
        if (other is ItemObject item)
        {
            if (UnitData.Collectable && HoldingItem == null && item.IsInteractable(Team))
            {
                HoldingItem = item;
                item.OnPickedUp(this);
                item.transform.SetParent(itemHolder);
                item.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            Debug.LogError("This method only handles item interaction");
        }
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} dead");
        if (HoldingItem != null)
        {
            HoldingItem.OnDestroyed();
            HoldingItem = null;
        }

        matchManager.EventBus.Unit.PublishUnitDead(this);

        matchManager.RespawnUnit(this);
        OnUnitDead?.Invoke(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(GlobalPos, UnitData.CollisionBound.Width / 2);
    }
}
