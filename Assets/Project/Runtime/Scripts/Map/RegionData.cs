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
                    IsUniqueInstanceConstraint = IsLocked,
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

public static class RegionUtils
{
    /// <summary>
    /// y = x 대각선 기준 선대칭된 영역 데이터 생성.
    /// (Team A 데이터를 넣으면 Team B 데이터가 반환됨)
    /// </summary>
    public static RegionData CreateSymmetricRegion(RegionData source, TeamData targetTeam)
    {
        var mirror = new RegionData();

        mirror.Name = source.Name.Replace("TeamA", "TeamB").Replace("_A", "_B");
        mirror.Team = targetTeam;
        mirror.Type = source.Type;

        // 성소
        mirror.InputUnitType = source.InputUnitType;
        mirror.OutputUnitType = source.OutputUnitType;
        mirror.IsLocked = source.IsLocked;

        if (source.Area != null)
        {
            mirror.Area = new RectInt[source.Area.Length];
            for (int i = 0; i < source.Area.Length; i++)
            {
                RectInt srcRect = source.Area[i];

                // y=x 대칭
                mirror.Area[i] = new RectInt(srcRect.y, srcRect.x, srcRect.height, srcRect.width);
            }
        }

        return mirror;
    }
}
