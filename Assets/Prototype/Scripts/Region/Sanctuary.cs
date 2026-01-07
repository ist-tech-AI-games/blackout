public class Sanctuary : MapRegion
{
    public UnitData TargetInputData;
    public UnitData ResultOutputData;

    public bool IsUniqueInstanceConstraint;

    private Unit lockedUnit = null;

    public override void OnUnitEnter(Unit unit)
    {
        if (IsUniqueInstanceConstraint && lockedUnit != null) return;

        if (unit.UnitData == TargetInputData)
        {
            PerformTransformation(unit);
        }
    }

    private void PerformTransformation(Unit unit)
    {
        unit.SetUnitClass(ResultOutputData);
        
        if (IsUniqueInstanceConstraint)
        {
            lockedUnit = unit;
            
            unit.OnUnitDead += OnLockedUnitDead;
        }
    }

    private void OnLockedUnitDead(Unit unit)
    {
        if (unit == lockedUnit)
        {
            unit.OnUnitDead -= OnLockedUnitDead;
            lockedUnit = null;
        }
    }
}