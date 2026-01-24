using UnityEngine;

public class ItemObject : MonoBehaviour, IMapObject
{
    public enum ItemState
    {
        OnGround,
        Carried,
    }

    [field: SerializeField]
    public ItemData ItemData { get; private set; }

    [field: SerializeField]
    public int ItemAmount { get; private set; } = 1;

    public CollisionBound CollisionBound => ItemData.CollisionBound;
    public Vector2 GlobalPos => transform.position;
    public MapTile OwnedTile { get; set; }

    public ItemState State { get; private set; } = ItemState.OnGround;

    private TeamContext currentAppliedContext;
    private GameManager gameManager;
    private MapManager mapManager;

    public void Initialize(GameManager gameManager, MapManager mapManager)
    {
        this.gameManager = gameManager;
        this.mapManager = mapManager;
        AddToMap(mapManager.GetTileAtWorldPos(GlobalPos));
    }

    // === State Machine Transitions ===

    public void OnPickedUp(Unit unit)
    {
        if (State == ItemState.Carried)
            return;

        RemoveEffect();
        RemoveFromMap();

        State = ItemState.Carried;
        // TODO: transform parenting
    }

    public void OnDropped(MapTile targetTile)
    {
        State = ItemState.OnGround;
        transform.SetParent(gameManager.GetItemParent());
        transform.position = mapManager.CellToCenterWorld(targetTile.CellPos);

        AddToMap(targetTile);
    }

    public void OnDestroyed()
    {
        RemoveEffect();
        RemoveFromMap();
        Destroy(gameObject);
    }

    // === Map Object ===

    private void AddToMap(MapTile tile)
    {
        OwnedTile = tile;
        tile.MapObjects.Add(this);

        ApplyEffect(tile.OwnedRegion);
    }

    private void RemoveFromMap()
    {
        if (OwnedTile != null)
        {
            OwnedTile.MapObjects.Remove(this);
            OwnedTile = null;
        }
    }

    // === Amount ===

    public void UpdateAmount(int newAmount)
    {
        if (newAmount == ItemAmount)
            return;

        if (currentAppliedContext != null)
        {
            ItemData.Effect.ExitEffect(currentAppliedContext, ItemAmount);
            ItemData.Effect.EnterEffect(currentAppliedContext, newAmount);
        }
        ItemAmount = newAmount;
    }

    // === Effects ===

    public void ApplyEffect(MapRegion region)
    {
        // remove old effect
        if (currentAppliedContext != null && currentAppliedContext.Team != region.OwnedTeam)
            RemoveEffect();

        TeamContext newContext = gameManager.GetTeamContext(region.OwnedTeam);

        if (newContext != null && ItemData.Effect != null)
        {
            ItemData.Effect.EnterEffect(newContext, ItemAmount);
            currentAppliedContext = newContext;
        }
    }

    private void RemoveEffect()
    {
        if (currentAppliedContext != null && ItemData.Effect != null)
        {
            ItemData.Effect.ExitEffect(currentAppliedContext, ItemAmount);
            currentAppliedContext = null;
        }
    }

    public void OnOverlapped(IMapObject other) { /* nop */}

    public bool IsInteractable(TeamData team)
    {
        switch (ItemData.InteractionOption)
        {
            case ObjectInteractionOption.All:
                return true;
            case ObjectInteractionOption.IgnoreFriend:
                return OwnedTile.OwnedRegion == null || OwnedTile.OwnedRegion.OwnedTeam != team;
            case ObjectInteractionOption.IgnoreEnemy:
                return OwnedTile.OwnedRegion == null || OwnedTile.OwnedRegion.OwnedTeam != gameManager.OpponentTeam(team);
            default: // None
                return false;
        }
    }
}
