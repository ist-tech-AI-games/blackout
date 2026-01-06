using UnityEngine;
using UnityEngine.Tilemaps;

public class MapData
{
    public int Width => MapTiles.GetLength(1);
    public int Height => MapTiles.GetLength(0);
    public MapTile[,] MapTiles { get; private set; }
    public MapRegion[] MapRegions { get; private set; }
    public Tilemap LevelTilemap { get; private set; }
    public Vector2Int TopLeft { get; private set; }

    public MapData(MapTile[,] mapTiles, MapRegion[] mapRegions, Tilemap levelTilemap, Vector2Int topleft)
    {
        MapTiles = mapTiles;
        MapRegions = mapRegions;
        LevelTilemap = levelTilemap;
        TopLeft = topleft;
    }

    public MapTile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null; 
        return MapTiles[y, x];
    }
}
