using System;
using UnityEngine;

public enum CollisionBoundType
{
    Circle,
    Square,
}

[Serializable]
public struct CollisionBound
{
    public CollisionBoundType Type;
    public float Width;

    public float HalfWidth => Width / 2;
}

public static class CollisionUtils
{
    public static bool IsOverlapping(
        Vector2 posA, CollisionBound boundA,
        Vector2 posB, CollisionBound boundB
    )
    {
        float distSqr = (posA - posB).sqrMagnitude;
        float maxReach = boundA.HalfWidth * 1.5f + boundB.HalfWidth * 1.5f; // > sqrt(2)
        if (distSqr > maxReach * maxReach)
            return false;

        if (boundA.Type == CollisionBoundType.Circle)
            return boundB.Type == CollisionBoundType.Circle
                ? CheckCircleCircle(posA, boundA.HalfWidth, posB, boundB.HalfWidth)
                : CheckCircleAABB(posA, boundA.HalfWidth, posB, boundB.HalfWidth);
        else
            return boundB.Type == CollisionBoundType.Circle
                ? CheckCircleAABB(posB, boundB.HalfWidth, posA, boundA.HalfWidth)
                : CheckAABBAABB(posA, boundA.HalfWidth, posB, boundB.HalfWidth);
    }

    public static Vector2 GetClampedPosition(
        Vector2 moverPos, CollisionBound moverBound,
        Vector2 staticPos, CollisionBound staticBound
    )
    {
        if (
            moverBound.Type == CollisionBoundType.Circle
            && staticBound.Type == CollisionBoundType.Square
        )
        {
            Vector2 closestPointOnBox = GetClosestPointOnAABB(
                staticPos,
                staticBound.HalfWidth,
                moverPos
            );

            // closest point to center
            Vector2 diff = moverPos - closestPointOnBox;
            float sqrDist = diff.sqrMagnitude;

            // overlap
            if (sqrDist < moverBound.HalfWidth * moverBound.HalfWidth)
            {
                // cannot determine direction => arbitrary choose
                if (sqrDist <= Mathf.Epsilon * Mathf.Epsilon)
                    diff = Vector2.up;

                return closestPointOnBox + diff.normalized * moverBound.HalfWidth;
            }
        }

        else if (
            moverBound.Type == CollisionBoundType.Circle
            && staticBound.Type == CollisionBoundType.Circle
        )
        {
            Vector2 dir = moverPos - staticPos;
            float dist = dir.magnitude;
            float minDist = moverBound.HalfWidth + staticBound.HalfWidth;

            if (dist < minDist)
            {
                if (dist <= Mathf.Epsilon)
                    dir = Vector2.up;
                return staticPos + dir.normalized * minDist;
            }
        }

        else
            Debug.LogError($"Not supported collision: mover({moverBound.Type}) + static({staticBound.Type}). This is just a prototype!");

        return moverPos;
    }

    private static bool CheckCircleCircle(Vector2 posA, float rA, Vector2 posB, float rB)
    {
        float distSqr = (posA - posB).sqrMagnitude;
        float rSum = rA + rB;
        return distSqr <= rSum * rSum;
    }

    private static bool CheckAABBAABB(Vector2 posA, float extA, Vector2 posB, float extB)
    {
        bool xOverlap = Mathf.Abs(posA.x - posB.x) <= (extA + extB);
        bool yOverlap = Mathf.Abs(posA.y - posB.y) <= (extA + extB);
        return xOverlap && yOverlap;
    }

    private static bool CheckCircleAABB(
        Vector2 circlePos,
        float circleR,
        Vector2 boxPos,
        float boxExtent
    )
    {
        Vector2 closest = GetClosestPointOnAABB(boxPos, boxExtent, circlePos);

        float distSqr = (closest - circlePos).sqrMagnitude;
        return distSqr <= circleR * circleR;
    }

    private static Vector2 GetClosestPointOnAABB(Vector2 boxCenter, float boxExtent, Vector2 point)
    {
        float minX = boxCenter.x - boxExtent;
        float maxX = boxCenter.x + boxExtent;
        float minY = boxCenter.y - boxExtent;
        float maxY = boxCenter.y + boxExtent;

        float closestX = Mathf.Clamp(point.x, minX, maxX);
        float closestY = Mathf.Clamp(point.y, minY, maxY);

        return new Vector2(closestX, closestY);
    }
}
