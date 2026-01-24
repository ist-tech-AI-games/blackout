using UnityEngine;

public class UnitMovementSystem
{
    private readonly IMapCollisionContext mapContext;
    private readonly TeamData team;
    private UnitData unitData;
    
    private static readonly Vector2Int[] neighborDelta = new Vector2Int[]
    {
        new(-1, -1), new(-1, 0), new(-1, 1),
        new(0, -1),              new(0, 1),
        new(1, -1),  new(1, 0),  new(1, 1),
    };

    public UnitMovementSystem(IMapCollisionContext mapContext, UnitData unitData, TeamData team)
    {
        this.mapContext = mapContext;
        this.unitData = unitData;
        this.team = team;
    }

    public void SetUnitClass(UnitData unitData)
    {
        this.unitData = unitData;
    }

    /// <summary>
    /// 입력과 델타타임을 기반으로 충돌을 적용한 다음 위치를 계산.
    /// </summary>
    public Vector2 CalculateNextPosition(Vector2 currentPos, Vector2 inputDir, float deltaTime)
    {
        Vector2 displacement = inputDir * unitData.Speed * deltaTime;
        return ClampPosition(currentPos, displacement);
    }

    private Vector2 ClampPosition(Vector2 currentPos, Vector2 displacement)
    {
        Vector2 desired = currentPos + displacement;
        Vector2Int currentCell = mapContext.WorldToCell(currentPos);

        foreach (Vector2Int delta in neighborDelta)
        {
            float xx = delta.x * displacement.x;
            float yy = delta.y * displacement.y;

            // 이동 방향에 맞지 않는 주변 타일은 검사 불필요.
            if (xx >= 0 && yy >= 0 && xx + yy > 0)
            {
                Vector2Int checkCell = currentCell + delta;

                if (!mapContext.IsWalkable(checkCell, team))
                {
                    CollisionBound tileBound = mapContext.GetTileCollisionBound(checkCell);
                    Vector3 tileCenter = mapContext.CellToCenterWorld(checkCell);

                    desired = CollisionUtils.GetClampedPosition(
                        desired,
                        unitData.CollisionBound,
                        tileCenter,
                        tileBound
                    );
                }
            }
        }
        return desired;
    }
}