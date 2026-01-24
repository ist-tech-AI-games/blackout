using UnityEngine;

public interface IMapObject
{
    public CollisionBound CollisionBound { get; }
    public MapTile OwnedTile { get; set; }
    public Vector2 GlobalPos { get; }
    public void Initialize(GameManager gameManager, MapManager mapManager);
    public void OnOverlapped(IMapObject other);
}