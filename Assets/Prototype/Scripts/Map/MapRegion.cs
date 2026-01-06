using System.Collections.Generic;

public class MapRegion
{
    public TeamData OwnedTeam { get; set; }
    public List<MapTile> MapTiles { get; private set; } = new();
}
