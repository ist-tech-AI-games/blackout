using System.Linq;
using UnityEngine;

public class UnitInteractionSystem
{
    private readonly IMapInteractionContext mapContext;
    private readonly Unit ownerUnit;

    private static readonly Vector2Int[] neighborDelta = new Vector2Int[]
    {
        new(-1, -1), new(-1, 0), new(-1, 1),
        new(0, -1),              new(0, 1),
        new(1, -1),  new(1, 0),  new(1, 1),
    };

    public UnitInteractionSystem(IMapInteractionContext context, Unit owner)
    {
        mapContext = context;
        ownerUnit = owner;
    }

    // 이동 이후 상호작용 처리
    public void ProcessInteractions(Vector2 currentWorldPos)
    {
        Vector2Int currentCell = mapContext.WorldToCell(currentWorldPos);

        // 1. 주변 타일 탐색
        foreach (Vector2Int delta in neighborDelta)
        {
            Vector2Int checkCell = currentCell + delta;
            
            if (mapContext.IsWalkable(checkCell, ownerUnit.Team))
            {
                var objectsOnTile = mapContext.GetObjectsAt(checkCell);
                if (objectsOnTile == null) continue;

                // 리스트가 수정될 수 있으므로 역순 순회
                for (int i = objectsOnTile.Count - 1; i >= 0; i--)
                {
                    IMapObject targetObj = objectsOnTile[i];
                    if (targetObj == (IMapObject)ownerUnit) continue;

                    if (CollisionUtils.IsOverlapping(
                        targetObj.GlobalPos, targetObj.CollisionBound,
                        currentWorldPos, ownerUnit.CollisionBound))
                    {
                        ResolveInteraction(targetObj);
                    }
                }
            }
        }
    }

    private void ResolveInteraction(IMapObject other)
    {
        if (other is Unit otherUnit)
        {
            if (ownerUnit.Team.IsOpponent(otherUnit.Team))
            {
                ResolveCombat(ownerUnit, otherUnit);
            }
        }
        else if (other is ItemObject item)
        {
            // 아이템 획득 로직...
            // Unit 클래스에 public 메서드(PickUpItem 등)를 만들어 호출하거나
            // ownerUnit.OnOverlapped(item)을 호출하여 위임
            ownerUnit.OnOverlapped(item); 
        }
    }

    private void ResolveCombat(Unit attacker, Unit defender)
    {
        bool attackerWins = attacker.UnitData.Beats.Contains(defender.UnitData);
        bool defenderWins = defender.UnitData.Beats.Contains(attacker.UnitData);

        if (attackerWins && defenderWins)
        {
            attacker.Die();
            defender.Die();
        }
        else if (attackerWins)
        {
            defender.Die();
        }
        else if (defenderWins)
        {
            attacker.Die();
        }
    }
}