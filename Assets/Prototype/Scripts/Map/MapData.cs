using UnityEngine;
using UnityEngine.Tilemaps;

public class MapSpaceInfo
{
    public Tilemap LevelTilemap { get; private set; }
    public Vector2Int BottomLeft { get; private set; }
    public Vector2Int TeamASpawnPoint { get; private set; }
    public Vector2Int TeamBSpawnPoint { get; private set; }

    public MapSpaceInfo(Tilemap levelTilemap, Vector2Int bottomLeft, Vector2Int teamASpawnPoint, Vector2Int teamBSpawnPoint)
    {
        LevelTilemap = levelTilemap;
        BottomLeft = bottomLeft;
        TeamASpawnPoint = teamASpawnPoint;
        TeamBSpawnPoint = teamBSpawnPoint;
    }
}

public class MapData
{
    public int Width => MapTiles.GetLength(1);
    public int Height => MapTiles.GetLength(0);
    public MapTile[,] MapTiles { get; private set; }
    public MapRegion[] MapRegions { get; private set; }
    public MapSpaceInfo MapSpaceInfo { get; private set; }

    public MapData(MapTile[,] mapTiles, MapRegion[] mapRegions, MapSpaceInfo mapSpaceInfo)
    {
        MapTiles = mapTiles;
        MapRegions = mapRegions;
        MapSpaceInfo = mapSpaceInfo;
    }

    public MapTile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null; 
        return MapTiles[y, x];
    }
}
