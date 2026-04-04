using System;
using System.Linq;
using UnityEngine;

public enum StatType
{
    MoveSpeed,
    // ...
    // 유형을 추가할 수는 있으나, 시스템 적용 필요.
}

public enum ModifierOperation
{
    Add,
    Multiply,
    Override
}

// 아이템은 창고에 있을 때 효과가 발생함.
// 창고를 기준으로 어느 팀에 효과를 적용하는지 설정.
public enum EffectTargetStrategy
{
    OwnerTeam,
    OpponentTeam,
    AllTeams
}

// 조건 명세. 현재는 유닛 유형만 따름.
[Serializable]
public struct ModifierCondition
{
    [Tooltip("특정 유닛 클래스에게만 적용하려면 할당. 비워두면 모든 유닛 적용.")]
    public UnitData[] TargetClasses;

    // 유닛이 이 조건을 만족하는지 검사
    public bool IsMatch(Unit unit)
    {
        if (TargetClasses == null || TargetClasses.Length == 0) return true;
        return TargetClasses.Any(x => x == unit.UnitData);
    }
}

// 효과를 UI에 표시하기 위한 구조체.
[Serializable]
public struct EffectDisplayInfo
{
    public string Name;
    [TextArea] public string Description;
    public Sprite Icon;
    public bool IsHidden;
}