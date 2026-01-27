using UnityEngine;

public abstract class MapGenerator : MonoBehaviour
{
    public delegate void SpawnItemCallback(ItemData itemData, int amount, Vector2Int cellPos);
    public abstract MapData GenerateMapData();
    public abstract void SpawnItemObjects(MapManager mapManager, SpawnItemCallback spawnItemCallback);
}
