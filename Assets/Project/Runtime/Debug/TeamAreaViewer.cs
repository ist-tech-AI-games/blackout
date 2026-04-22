using UnityEngine;
using UnityEngine.Tilemaps;

public class TeamAreaViewer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TeamData teamA;
    [SerializeField] private TeamData teamB;

    [SerializeField] private TileBase teamATile;
    [SerializeField] private TileBase teamBTile;

    public void ColorTiles(MapData mapData)
    {
        foreach (var region in mapData.MapRegions)
        {
            foreach (var tile in region.MapTiles)
            {
                tilemap.SetTileFlags((Vector3Int)tile.CellPos, TileFlags.None);

                if (region is Storage)
                {
                    TileBase teamTile = region.OwnedTeam == teamA ? teamATile : teamBTile;
                    if (teamTile != null)
                    {
                        tilemap.SetTile((Vector3Int)tile.CellPos, teamTile);
                    }
                }

                tilemap.SetColor((Vector3Int)tile.CellPos, region.OwnedTeam.TeamColor);
            }
        }
    }
}
