using UnityEngine;
using UnityEngine.Tilemaps;

public class TeamAreaViewer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    public void ColorTiles(MapData mapData)
    {
        foreach (var region in mapData.MapRegions)
        {
            foreach (var tile in region.MapTiles)
            {
                tilemap.SetTileFlags((Vector3Int)tile.CellPos, TileFlags.None);
                tilemap.SetColor((Vector3Int)tile.CellPos, region.OwnedTeam.TeamColor);
            }
        }
    }
}
