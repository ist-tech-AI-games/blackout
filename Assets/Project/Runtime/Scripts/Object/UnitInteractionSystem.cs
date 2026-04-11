using System.Linq;
using UnityEngine;

public class UnitInteractionSystem
{
    private readonly IMapInteractionContext mapContext;
    private readonly Unit ownerUnit;

    private static readonly Vector2Int[] neighborDelta = new Vector2Int[]
    {
        new(-1, -1), new(-1, 0), new(-1, 1),
        new(0, -1),  new(0, 0),  new(0, 1),
        new(1, -1),  new(1, 0),  new(1, 1),
    };

    public UnitInteractionSystem(IMapInteractionContext context, Unit owner)
    {
        mapContext = context;
        ownerUnit = owner;
    }

    // Handle interactions after movement
    public void ProcessInteractions(Vector2 currentWorldPos)
    {
        Vector2Int currentCell = mapContext.WorldToCell(currentWorldPos);

        // 1. Scan neighboring tiles
        foreach (Vector2Int delta in neighborDelta)
        {
            Vector2Int checkCell = currentCell + delta;
            
            if (mapContext.IsWalkable(checkCell, ownerUnit.Team))
            {
                var objectsOnTile = mapContext.GetObjectsAt(checkCell);
                if (objectsOnTile == null) continue;

                // Snapshot before iterating: combat resolution (Die → Teleport) removes units
                // from this live list, so reverse iteration alone is insufficient when 2+ units
                // are removed (e.g. draw combat where both attacker and defender die).
                var snapshot = objectsOnTile.ToArray();
                foreach (IMapObject targetObj in snapshot)
                {
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
            ownerUnit.OnOverlapped(item);
        }
    }

    private void ResolveCombat(Unit attacker, Unit defender)
    {
        bool attackerWins = attacker.UnitData.Beats.Contains(defender.UnitData);
        bool defenderWins = defender.UnitData.Beats.Contains(attacker.UnitData);

        if (attackerWins && defenderWins)
        {
            attacker.Die(null);
            defender.Die(null);
        }
        else if (attackerWins)
        {
            defender.Die(attacker);
        }
        else if (defenderWins)
        {
            attacker.Die(defender);
        }
    }
}