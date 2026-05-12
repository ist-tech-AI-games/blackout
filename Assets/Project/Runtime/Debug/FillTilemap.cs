using UnityEngine;
using UnityEngine.Tilemaps;

public class FillTilemap : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tileToFill;
    [SerializeField] private RectInt area;

    [ContextMenu("Fill Tiles")]
    public void Fill()
    {
        for (int x = area.xMin; x < area.xMax; x++)
            for (int y = area.yMin; y < area.yMax; y++)
                tilemap.SetTile(new(x, y, 0), tileToFill);
    }
}
