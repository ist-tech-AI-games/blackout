using System;
using System.Collections.Generic;

public class Sanctuary : MapRegion
{
    public UnitData TargetInputData;
    public UnitData ResultOutputData;

    // 이 성소로 변신해 아직 그 클래스로 남아 있는 유닛의 팀당 최대 수. 0이면 제한 없음.
    public int MaxUnitsPerTeam;

    private readonly HashSet<Unit> transformedUnits = new();

    public override void OnUnitEnter(Unit unit)
    {
        if (MaxUnitsPerTeam > 0 && CountTransformed(unit.Team) >= MaxUnitsPerTeam) return;

        bool success = unit.TryTransform(TargetInputData, ResultOutputData);

        if (success && MaxUnitsPerTeam > 0)
            Track(unit);
    }

    private int CountTransformed(TeamData team)
    {
        int count = 0;
        foreach (var unit in transformedUnits)
            if (unit.Team == team)
                count++;
        return count;
    }

    // 사망 후 부활이나 에피소드 리셋으로 클래스가 바뀌면 자리를 비움.
    private void Track(Unit unit)
    {
        transformedUnits.Add(unit);

        Action<UnitData> onClassChanged = null;
        onClassChanged = newClass =>
        {
            if (newClass == ResultOutputData) return;
            unit.OnClassChanged -= onClassChanged;
            transformedUnits.Remove(unit);
        };
        unit.OnClassChanged += onClassChanged;
    }
}
