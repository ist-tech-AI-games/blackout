using System.Collections.Generic;
using UnityEngine;

public interface IMapCollisionContext
{
    bool IsWalkable(Vector2Int cellPos, TeamData team);

    Vector2Int WorldToCell(Vector3 worldPos);
    Vector3 CellToCenterWorld(Vector2Int cellPos);

    CollisionBound GetTileCollisionBound(Vector2Int cellPos);
}

public interface IMapInteractionContext : IMapCollisionContext
{
    List<IMapObject> GetObjectsAt(Vector2Int cellPos);
}