using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Item effect that applies stat modifiers (buffs/debuffs) to target teams while in storage.
/// Modifiers are added when entering storage and removed when exiting (unless absorbed).
/// </summary>
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

    public override void OnEnterStorage(ItemEffectContext context)
    {
        StatModifier modifier = CreateModifier(context.EffectSource);
        List<TeamContext> targets = context.ResolveTargetContexts(TargetStrategy);

        foreach (TeamContext target in targets)
        {
            context.AddModifier(target, modifier);
        }
    }

    public override void OnExitStorage(ItemEffectContext context)
    {
        // Modifiers are automatically removed by ItemObject before this is called.
        // No additional cleanup needed - modifiers disappear on all exit reasons including absorption.
    }
}