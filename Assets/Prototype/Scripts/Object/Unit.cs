using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a controllable unit (Worker, Guard, Carrier) in the game.
/// Handles movement, item interaction, class transformation, and stat calculations with caching.
/// Subscribes to team modifier events for automatic stat cache invalidation.
/// </summary>
public class Unit : MonoBehaviour, IMapObject, IResettable
{
    /// <summary>
    /// Invoked when the unit's class (UnitData) changes.
    /// </summary>
    public event Action<UnitData> OnClassChanged;

    /// <summary>
    /// Invoked when the unit dies.
    /// </summary>
    public event Action<Unit> OnUnitDead;

    /// <summary>
    /// Current unit class data (Worker, Guard, or Carrier).
    /// </summary>
    [field: SerializeField]
    public UnitData UnitData { get; private set; }

    /// <summary>
    /// Team this unit belongs to (Team A or Team B).
    /// </summary>
    [field: SerializeField]
    public TeamData Team { get; private set; }

    /// <summary>
    /// Collision bound from the current unit class.
    /// </summary>
    public CollisionBound CollisionBound => UnitData.CollisionBound;

    /// <summary>
    /// Current world position of the unit.
    /// </summary>
    public Vector2 GlobalPos => transform.position;

    /// <summary>
    /// The tile this unit is currently on.
    /// </summary>
    public MapTile OwnedTile { get; set; }

    [SerializeField]
    private Transform itemHolder;

    // dependencies
    private MatchManager matchManager;
    private MapManager mapManager;
    private UnitMovementSystem unitMovementSystem;
    private UnitInteractionSystem unitInteractionSystem;
    private TeamContext teamContext;

    // Stat caching for performance
    private Dictionary<StatType, float> cachedStats = new Dictionary<StatType, float>();

    /// <summary>
    /// The item currently held by this unit, or null if not holding any item.
    /// </summary>
    public ItemObject HoldingItem { get; private set; } = null;

    /// <summary>
    /// Current movement speed including team modifiers.
    /// Uses cached stat calculation for performance.
    /// </summary>
    public float MoveSpeed
    {
        get
        {
            // 주의: 초기화 이후에 쓰지 않으면 적용 안 됨.
            if (matchManager == null) return UnitData.BaseSpeed;

            return GetCachedStat(StatType.MoveSpeed, UnitData.BaseSpeed);
        }
    }

    /// <summary>
    /// Gets a stat value from cache, calculating it if not cached.
    /// Cache is invalidated automatically when team modifiers change.
    /// </summary>
    private float GetCachedStat(StatType statType, float baseValue)
    {
        if (cachedStats.TryGetValue(statType, out float cachedValue))
        {
            return cachedValue;
        }

        // Calculate and cache
        if (teamContext == null)
            teamContext = matchManager.GetTeamContext(Team);

        if (teamContext == null)
            return baseValue;

        float calculatedValue = teamContext.CalculateStat(this, statType, baseValue);
        cachedStats[statType] = calculatedValue;

        return calculatedValue;
    }

    /// <summary>
    /// Invalidates stat cache when modifiers change.
    /// Called automatically via TeamContext events.
    /// </summary>
    private void InvalidateStatCache()
    {
        cachedStats.Clear();
    }

    /// <summary>
    /// Initializes the unit with required dependencies.
    /// Sets up movement and interaction systems, subscribes to team modifier events.
    /// </summary>
    /// <param name="matchManager">Match manager for team context access.</param>
    /// <param name="mapManager">Map manager for movement calculations.</param>
    public void Initialize(MatchManager matchManager, MapManager mapManager)
    {
        this.matchManager = matchManager;
        this.mapManager = mapManager;
        unitMovementSystem = new(mapManager, Team);
        unitInteractionSystem = new(mapManager, this);

        // Subscribe to modifier events for stat cache invalidation
        teamContext = matchManager.GetTeamContext(Team);
        if (teamContext != null)
        {
            teamContext.OnModifierAdded += OnModifierChanged;
            teamContext.OnModifierRemoved += OnModifierChanged;
        }
    }

    /// <summary>
    /// Called when a modifier is added or removed from the team.
    /// Invalidates stat cache to ensure consistency.
    /// </summary>
    private void OnModifierChanged(StatModifier modifier)
    {
        InvalidateStatCache();
    }

    /// <summary>
    /// Resets unit state to initial condition.
    /// Destroys held item, reactivates game object, and clears stat cache.
    /// </summary>
    public void ResetState()
    {
        if (HoldingItem != null)
        {
            HoldingItem.OnDestroyed();
            HoldingItem = null;
        }

        gameObject.SetActive(true);
        OwnedTile = null;

        // Clear stat cache on reset (modifiers will be reapplied)
        InvalidateStatCache();
    }

    /// <summary>
    /// Unity lifecycle method - cleanup event subscriptions to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (teamContext != null)
        {
            teamContext.OnModifierAdded -= OnModifierChanged;
            teamContext.OnModifierRemoved -= OnModifierChanged;
        }
    }

    /// <summary>
    /// Attempts to transform this unit to a different class.
    /// Called by Sanctuary regions when unit enters.
    /// </summary>
    /// <param name="requiredInput">Required current class for transformation.</param>
    /// <param name="targetOutput">Target class after transformation.</param>
    /// <returns>True if transformation succeeded (unit had required class).</returns>
    public bool TryTransform(UnitData requiredInput, UnitData targetOutput)
    {
        if (UnitData != requiredInput)
            return false;

        SetUnitClass(targetOutput);
        return true;
    }

    /// <summary>
    /// Retrieves the item currently held by this unit.
    /// Called by Storage regions to take the item (transfer, not borrow).
    /// </summary>
    /// <returns>The held item, or null if not holding any item.</returns>
    public ItemObject RetrieveItem()
    {
        if (HoldingItem == null)
            return null;

        ItemObject item = HoldingItem;
        HoldingItem = null;

        // (선택사항) 아이템 부모 관계 해제 등은 ItemObject.OnDropped에서 처리됨
        return item;
    }

    /// <summary>
    /// Changes the unit's class (Worker, Guard, Carrier).
    /// Destroys held item if new class can't collect items.
    /// Invalidates stat cache and invokes OnClassChanged event.
    /// </summary>
    /// <param name="unitData">New unit class data to apply.</param>
    public void SetUnitClass(UnitData unitData)
    {
        UnitData = unitData;
        if (!unitData.Collectable && HoldingItem != null)
        {
            HoldingItem.OnDestroyed();
            HoldingItem = null;
        }
        InvalidateStatCache();
        OnClassChanged?.Invoke(unitData);
    }

    /// <summary>
    /// Moves the unit based on input direction and delta time.
    /// Handles collision detection and processes tile/region interactions.
    /// </summary>
    /// <param name="input">Movement input direction (will be normalized).</param>
    /// <param name="deltaTime">Time delta for frame-rate independent movement.</param>
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

    /// <summary>
    /// Teleports the unit to a specific cell position.
    /// WARNING: Does not check tile collision - ensure target position is valid before calling.
    /// </summary>
    /// <param name="pos">Cell position to teleport to.</param>
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

    /// <summary>
    /// Handles overlap interaction with other map objects.
    /// Currently only handles item pickup interaction.
    /// </summary>
    /// <param name="other">The map object this unit overlapped with.</param>
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

    /// <summary>
    /// Handles unit death by destroying held item and triggering respawn.
    /// Publishes unit death event and invokes OnUnitDead.
    /// </summary>
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
