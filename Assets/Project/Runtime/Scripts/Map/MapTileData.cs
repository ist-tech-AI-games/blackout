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

/// <summary>
/// Configuration data for map tiles.
/// Defines the visual tile, collision behavior, and collision bounds.
/// </summary>
[CreateAssetMenu(fileName="New MapTileData", menuName="Project/Map Tile Data")]
public class MapTileData : ScriptableObject
{
    [field: SerializeField] public TileBase TileData { get; private set; }
    [field: SerializeField] public TileCollisionOption TileCollisionOption { get; private set; }
    [field: SerializeField] public CollisionBound CollisionBound { get; private set; }

    /// <summary>
    /// Validates configuration values when changed in the Unity Inspector.
    /// Ensures TileData is assigned (required for tilemap rendering).
    /// </summary>
    private void OnValidate()
    {
        // TileData (TileBase) is required for rendering
        if (TileData == null)
        {
            Debug.LogWarning($"[MapTileData:{name}] TileData (TileBase) is not assigned. This tile will not render on the tilemap.", this);
        }
    }
}