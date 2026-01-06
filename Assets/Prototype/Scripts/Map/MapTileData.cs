using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileCollisionOption
{
    Pass,
    // These two are considered as `Pass` when the team is not specified or is neutral.
    BlockFriendly,
    BlockEnemy,
    BlockAll,
}

[CreateAssetMenu(fileName="New MapTileData", menuName="Project/Map Tile Data")]
public class MapTileData : ScriptableObject
{
    [field: SerializeField] public TileBase TileData { get; private set; }
    [field: SerializeField] public TileCollisionOption TileCollisionOption { get; private set; }
    [field: SerializeField] public CollisionBound CollisionBound { get; private set; }
}