using UnityEngine;

[CreateAssetMenu(menuName = "Project/Item Effects/Buff")]
public class BuffItemEffect : ItemEffect
{
    [Header("Logic")]
    public StatType StatType;
    public float Value;
    public ModifierOperation Operation = ModifierOperation.Multiply;

    [Tooltip("조건 (특정 클래스 전용 등)")]
    public ModifierCondition Condition;

    [Tooltip("팀 조건 (창고 기준)")]
    public EffectTargetStrategy TargetStrategy = EffectTargetStrategy.OwnerTeam;

    [Header("Visualization")]
    public EffectDisplayInfo DisplayInfo;

    public StatModifier CreateModifier(object source)
    {
        return new StatModifier(StatType, Value, Operation, Condition, DisplayInfo, source);
    }

    // 어쩌다 보니 Liskov 치환 원칙 위반이지만 호환성 유지함.
    public override void EnterEffect(TeamContext context, int amount) { }
    public override void ExitEffect(TeamContext context, int amount, ItemExitReason reason) { }
}