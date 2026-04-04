using System;

public class StatModifier
{
    public readonly string ID;

    // Type 스탯을 조건에 맞게 적용하는 효과 구조.
    public readonly StatType Type;
    public readonly float Value;
    public readonly ModifierOperation Operation;
    public readonly ModifierCondition Condition;

    // 시각화
    public readonly EffectDisplayInfo DisplayInfo;

    public readonly object Source;

    public StatModifier(
        StatType type,
        float value,
        ModifierOperation op,
        ModifierCondition condition,
        EffectDisplayInfo displayInfo,
        object source)
    {
        ID = Guid.NewGuid().ToString();
        Type = type;
        Value = value;
        Operation = op;
        Condition = condition;
        DisplayInfo = displayInfo;
        Source = source;
    }

    public bool IsMatch(Unit unit) => Condition.IsMatch(unit);
}