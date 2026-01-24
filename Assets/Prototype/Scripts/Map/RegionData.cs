using System;
using UnityEngine;

public enum RegionType
{
    Default,
    Sanctuary,
    Storage,
}

[Serializable]
public class RegionData
{
    public string Name;
    public TeamData Team;
    public RectInt[] Area;

    public RegionType Type;

    // storage (no additional fields)
    // sanctuary
    public UnitData InputUnitType;
    public UnitData OutputUnitType;
    public bool IsLocked;

    public MapRegion CreateRuntimeRegion()
    {
        MapRegion region;

        switch (Type)
        {
            case RegionType.Sanctuary:
                region = new Sanctuary()
                {
                    TargetInputData = InputUnitType,
                    ResultOutputData = OutputUnitType,
                    IsUniqueInstanceConstraint = IsLocked
                };
                break;
            case RegionType.Storage:
                region = new Storage();
                break;
            default:
                region = new MapRegion(); // Default
                break;
        }

        region.OwnedTeam = Team;
        return region;
    }
}