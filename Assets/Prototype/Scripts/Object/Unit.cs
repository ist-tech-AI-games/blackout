using System;
using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour, IMapObject
{
    // Events
    public event Action<UnitData> OnClassChanged;

    [field: SerializeField]
    public UnitData UnitData { get; private set; }

    [field: SerializeField]
    public TeamData Team { get; private set; }
    public CollisionBound CollisionBound => UnitData.CollisionBound;
    public MapTile OwnedTile { get; set; }
    public Vector2 GlobalPos => transform.position;

    [SerializeField] private Transform itemHolder;

    private GameManager gameManager;
    private ItemObject holdingItem = null;

    private static readonly Vector2Int[] neighborDelta = new Vector2Int[]
    {
        new(-1, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, -1),
        new(0, 1),
        new(1, -1),
        new(1, 0),
        new(1, 1),
    };

    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
        OwnedTile = gameManager.WorldToTile(transform.position);
        OwnedTile.OnObjectEnter(this);
    }

    public void SetUnitClass(UnitData unitData)
    {
        UnitData = unitData;
        if (!unitData.Collectable && holdingItem != null)
        {
            holdingItem.OnDestroyed();
            holdingItem = null;
        }
        OnClassChanged?.Invoke(unitData);
    }

    public void Move(Vector2 input)
    {
        Vector2 targetPosition = ClampPosition(input * UnitData.Speed);
        ChangePos(targetPosition);
        CheckObjectOverlap(targetPosition);
    }

    // NOTE: This does not check for the tile collision, so DON'T teleport to blocking tile.
    public void Teleport(Vector2Int pos)
    {
        ChangePos(gameManager.CellToCenterWorld(pos));
        CheckObjectOverlap(pos);
    }

    private void ChangePos(Vector2 targetPosition)
    {
        MapTile currentTile = gameManager.WorldToTile(targetPosition);

        if (OwnedTile != currentTile)
        {
            OwnedTile.OnObjectExit(this);
            currentTile.OnObjectEnter(this);
            OwnedTile = currentTile;
        }

        transform.position = targetPosition;
    }

    private Vector2 ClampPosition(Vector2 displacement)
    {
        Vector2 desired = (Vector2)transform.position + displacement;
        Vector2Int currentCell = gameManager.WorldToCell(transform.position);

        foreach (Vector2Int delta in neighborDelta)
        {
            float xx = delta.x * displacement.x;
            float yy = delta.y * displacement.y;
            if (
                xx >= 0 && yy >= 0 && xx + yy > 0
                && gameManager.CellToTile(currentCell + delta) is MapTile tile
                && !gameManager.CanPassThrough(tile, Team)
            )
            {
                desired = CollisionUtils.GetClampedPosition(
                    desired,
                    CollisionBound,
                    gameManager.CellToCenterWorld(currentCell + delta),
                    tile.TileData.CollisionBound
                );
            }

        }
        return desired;
    }

    public void OnOverlapped(IMapObject other)
    {
        Debug.Log($"Overlap: {this}, {other}");
        if (other is Unit otherUnit)
        {
            if (otherUnit.Team != Team)
            {
                UnitData otherUnitData = otherUnit.UnitData;
                if (UnitData.Beats.Contains(otherUnitData))
                    otherUnit.Die();
                // TODO: 더 근본적인 해결책 찾기
                if (otherUnitData.Beats.Contains(UnitData))
                    Die();
            }
        }

        else if (other is ItemObject item)
        {
            if (UnitData.Collectable && holdingItem == null && item.IsInteractable(Team))
            {
                holdingItem = item;
                item.OnPickedUp(this);
                item.transform.SetParent(itemHolder);
                item.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} dead");
        if (holdingItem != null)
        {
            holdingItem.OnDestroyed();
            holdingItem = null;
        }
        gameManager.RespawnUnit(this);
    }

    private void CheckObjectOverlap(Vector2 pos)
    {
        Vector2Int currentCell = gameManager.WorldToCell(pos);

        foreach (Vector2Int delta in neighborDelta)
            if (
                gameManager.CellToTile(currentCell + delta) is MapTile tile
                && gameManager.CanPassThrough(tile, Team)
            )
                for (int i = tile.MapObjects.Count - 1; i >= 0; i--)  // prevent mutating in foreach
                {
                    IMapObject mapObject = tile.MapObjects[i];
                    if (
                        !ReferenceEquals(mapObject, this)
                        && CollisionUtils.IsOverlapping(
                            mapObject.GlobalPos,
                            mapObject.CollisionBound,
                            pos,
                            CollisionBound
                        )
                    )
                    {
                        OnOverlapped(mapObject);
                        mapObject.OnOverlapped(this);
                    }
                }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(GlobalPos, UnitData.CollisionBound.Width / 2);
    }
}
