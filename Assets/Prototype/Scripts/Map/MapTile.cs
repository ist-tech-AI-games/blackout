using System.Collections.Generic;
using UnityEngine;

public class MapTile
{
    public MapTileData TileData { get; private set; }
    public MapRegion OwnedRegion { get; private set; }
    public Vector2Int CellPos { get; private set; }
    public List<IMapObject> MapObjects { get; private set; } = new();

    public MapTile(MapTileData tileData, MapRegion ownedRegion, Vector2Int cellPos)
    {
        TileData = tileData;
        OwnedRegion = ownedRegion;
        CellPos = cellPos;
    }

    public void OnObjectEnter(IMapObject mapObject)
    {
        MapObjects.Add(mapObject);
    } 

    public void OnObjectExit(IMapObject mapObject)
    {
        MapObjects.Remove(mapObject);
    }
}
