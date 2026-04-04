using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Represents an item (battery, buff item, etc.) in the game world.
/// Manages item state transitions (OnGround, Carried, InPool) and applies effects when entering/exiting storage.
/// Uses object pooling for efficient spawning/despawning.
/// Tracks applied modifiers for proper cleanup on state changes.
/// </summary>
public class ItemObject : MonoBehaviour, IMapObject, IResettable
{
    /// <summary>
    /// Invoked when item data or amount changes.
    /// </summary>
    public event Action<ItemData, int> OnDataUpdated;

    /// <summary>
    /// Possible states for an item object.
    /// </summary>
    public enum ItemState
    {
        /// <summary>Item is on the ground (can be picked up).</summary>
        OnGround,
        /// <summary>Item is being carried by a unit.</summary>
        Carried,
        /// <summary>Item is in the object pool (inactive).</summary>
        InPool
    }

    [Header("Data")]
    /// <summary>
    /// Configuration data for this item (Battery, Buff Item, etc.).
    /// </summary>
    [field: SerializeField] public ItemData ItemData { get; private set; }

    /// <summary>
    /// Current stack amount of this item.
    /// </summary>
    [field: SerializeField] public int ItemAmount { get; private set; } = 1;

    /// <summary>
    /// Collision bound from the item data.
    /// </summary>
    public CollisionBound CollisionBound => ItemData != null ? ItemData.CollisionBound : default;

    /// <summary>
    /// Current world position of the item.
    /// </summary>
    public Vector2 GlobalPos => transform.position;

    /// <summary>
    /// The tile this item is currently on.
    /// </summary>
    public MapTile OwnedTile { get; private set; }

    /// <summary>
    /// Current state of this item object.
    /// </summary>
    public ItemState State { get; private set; } = ItemState.InPool;

    // Internal State
    private TeamContext currentAppliedContext;
    private MatchManager matchManager;
    private MapManager mapManager;
    private IObjectPool<ItemObject> managedPool;
    private Transform worldParent;
    private List<(TeamContext context, StatModifier modifier)> appliedModifiers = new();

    /// <summary>
    /// Gets the team context this item is currently applied to (for debug/inspection).
    /// </summary>
    public TeamContext CurrentAppliedContext => currentAppliedContext;

    /// <summary>
    /// Gets all modifiers currently applied by this item (for debug/inspection).
    /// </summary>
    /// <returns>Read-only list of applied modifiers with their target contexts.</returns>
    public IReadOnlyList<(TeamContext context, StatModifier modifier)> GetAppliedModifiers()
        => appliedModifiers.AsReadOnly();

    // ===== Initialization & Lifecycle =====

    /// <summary>
    /// Initializes the item object with required dependencies.
    /// Called once when the item is created in the object pool.
    /// </summary>
    /// <param name="matchManager">Match manager for team context access.</param>
    /// <param name="mapManager">Map manager for tile positioning.</param>
    /// <param name="pool">Object pool managing this item.</param>
    /// <param name="worldParent">Transform parent for items on the ground.</param>
    public void Initialize(MatchManager matchManager, MapManager mapManager, IObjectPool<ItemObject> pool, Transform worldParent)
    {
        this.matchManager = matchManager;
        this.mapManager = mapManager;
        managedPool = pool;
        this.worldParent = worldParent;
    }

    /// <summary>
    /// Resets item state when retrieved from object pool.
    /// Clears applied context and sets state to OnGround.
    /// </summary>
    public void ResetState()
    {
        State = ItemState.OnGround;
        currentAppliedContext = null;
        OwnedTile = null;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Registers the item to the map at a specific position.
    /// Called when spawning items during map generation.
    /// </summary>
    /// <param name="itemData">Item configuration data.</param>
    /// <param name="amount">Initial stack amount.</param>
    /// <param name="cellPos">Cell position to spawn at.</param>
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

    /// <summary>
    /// Handles item being picked up by a unit.
    /// Transitions state to Carried and removes from map.
    /// </summary>
    /// <param name="unit">Unit that picked up this item.</param>
    public void OnPickedUp(Unit unit)
    {
        if (State == ItemState.Carried) return;

        matchManager.EventBus.Unit.PublishItemPickedUp(unit, this);

        ExitCurrentRegion(ItemExitReason.Dropped);
        RemoveFromMap();

        State = ItemState.Carried;
    }

    /// <summary>
    /// Handles item being dropped onto a tile.
    /// Transitions state to OnGround and updates position.
    /// </summary>
    /// <param name="targetTile">Tile to drop the item on.</param>
    public void OnDropped(MapTile targetTile)
    {
        State = ItemState.OnGround;
        MoveToTile(targetTile);
    }

    /// <summary>
    /// Handles item being absorbed by storage.
    /// Applies exit effects with Absorbed reason, then destroys without re-applying effects.
    /// </summary>
    public void OnAbsorbed()
    {
        ExitCurrentRegion(ItemExitReason.Absorbed);
        OnDestroyed(skipEffect: true);
    }

    /// <summary>
    /// Destroys the item and returns it to the object pool.
    /// Applies exit effects unless skipEffect is true.
    /// </summary>
    /// <param name="skipEffect">If true, skips effect application (used after absorption).</param>
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

    /// <summary>
    /// Updates the item's stack amount.
    /// Re-applies effects with new amount using Refresh exit reason.
    /// </summary>
    /// <param name="newAmount">New stack amount to set.</param>
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
        if (ItemData == null || ItemData.Effects == null || context == null) return;

        if (isEnter)
        {
            // Create context for entering storage
            ItemEffectContext effectContext = new ItemEffectContext(
                ownerContext: context,
                itemAmount: amount,
                matchManager: matchManager,
                effectSource: this,
                addModifier: AddModifierCallback,
                removeModifier: RemoveModifierCallback
            );

            foreach (var effect in ItemData.Effects)
            {
                if (effect == null) continue;
                effect.OnEnterStorage(effectContext);
            }
        }
        else
        {
            // Remove all tracked modifiers (always, even on absorption)
            for (int i = appliedModifiers.Count - 1; i >= 0; i--)
            {
                var (ctx, mod) = appliedModifiers[i];
                ctx.RemoveModifier(mod);
            }
            appliedModifiers.Clear();

            // Create context for exiting storage
            ItemEffectContext effectContext = ItemEffectContext.CreateForExit(
                ownerContext: context,
                itemAmount: amount,
                exitReason: reason,
                matchManager: matchManager,
                effectSource: this,
                addModifier: AddModifierCallback,
                removeModifier: RemoveModifierCallback
            );

            // Call exit logic for each effect
            foreach (var effect in ItemData.Effects)
            {
                if (effect == null) continue;
                effect.OnExitStorage(effectContext);
            }
        }
    }

    // Callback for ItemEffectContext to add modifiers (with tracking)
    private void AddModifierCallback(TeamContext targetContext, StatModifier modifier)
    {
        targetContext.AddModifier(modifier);
        appliedModifiers.Add((targetContext, modifier));

        // Debug.Log($"[ItemObject] Added modifier {modifier.Type} {modifier.Operation} {modifier.Value} to {targetContext.Team.name}. Total tracked: {appliedModifiers.Count}");
    }

    // Callback for ItemEffectContext to remove modifiers (with tracking)
    private void RemoveModifierCallback(TeamContext targetContext, StatModifier modifier)
    {
        targetContext.RemoveModifier(modifier);
        appliedModifiers.Remove((targetContext, modifier));

        // Debug.Log($"[ItemObject] Removed modifier {modifier.Type} {modifier.Operation} {modifier.Value} from {targetContext.Team.name}. Total tracked: {appliedModifiers.Count}");
    }

    /// <summary>
    /// Checks if this item can be interacted with (picked up) by the given team.
    /// Considers item's interaction option (All, IgnoreFriend, IgnoreEnemy) and current tile ownership.
    /// </summary>
    /// <param name="team">Team requesting interaction.</param>
    /// <returns>True if the team can interact with this item.</returns>
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

    /// <summary>
    /// Handles overlap interaction with other map objects.
    /// Currently no interaction logic needed for items.
    /// </summary>
    /// <param name="other">The map object this item overlapped with.</param>
    public void OnOverlapped(IMapObject other) { /* No interaction logic needed here yet */ }
}