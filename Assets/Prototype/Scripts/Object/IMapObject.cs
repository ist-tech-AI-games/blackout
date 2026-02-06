using UnityEngine;

public interface IMapObject
{
    public CollisionBound CollisionBound { get; }
    public MapTile OwnedTile { get; }
    public Vector2 GlobalPos { get; }
    public void OnOverlapped(IMapObject other);
}