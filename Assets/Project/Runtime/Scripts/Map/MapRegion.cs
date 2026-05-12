using System.Collections.Generic;

public class MapRegion
{
    public TeamData OwnedTeam { get; set; }
    public List<MapTile> MapTiles { get; protected set; } = new();
    public GameEventBus EventBus { get; set; }

    public virtual void OnUnitEnter(Unit unit) {}
    public virtual void OnUnitExit(Unit unit) {}
}
