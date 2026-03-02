using System;
using System.Collections.Generic;
using UnityEngine;

public class TeamContext
{
    public event Action<int> OnScoreChanged;
    public TeamData Team { get; private set; }
    public int Score { get; private set; } = 0;

    private List<StatModifier> activeModifiers = new();

    public event Action<StatModifier> OnModifierAdded;
    public event Action<StatModifier> OnModifierRemoved;

    /// <summary>
    /// Returns a read-only snapshot of currently active modifiers for debugging/inspection.
    /// </summary>
    public IReadOnlyList<StatModifier> GetActiveModifiers() => activeModifiers.AsReadOnly();
    
    public TeamContext(TeamData team)
    {
        Team = team;
    }

    // ===== 점수 관리 =====

    public void SetScore(int newValue, bool notify = true)
    {
        if (Score == newValue) return;

        Score = newValue;
        if (notify)
            OnScoreChanged?.Invoke(Score);
    }

    public void AddScore(int amount, bool notify = true) => SetScore(Score + amount, notify);

    // ===== Modifier 등록 / 해제 =====

    public void AddModifier(StatModifier modifier)
    {
        if (!activeModifiers.Contains(modifier))
        {
            activeModifiers.Add(modifier);
            OnModifierAdded?.Invoke(modifier);

            // Debug.Log($"[TeamContext:{Team.name}] Added modifier {modifier.Type} {modifier.Operation} {modifier.Value}. Active count: {activeModifiers.Count}");
        }
        else
        {
            // Debug.LogWarning($"[TeamContext:{Team.name}] Tried to add duplicate modifier {modifier.ID}");
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (activeModifiers.Contains(modifier))
        {
            activeModifiers.Remove(modifier);
            OnModifierRemoved?.Invoke(modifier);

            // Debug.Log($"[TeamContext:{Team.name}] Removed modifier {modifier.Type} {modifier.Operation} {modifier.Value}. Active count: {activeModifiers.Count}");
        }
        else
        {
            // Debug.LogWarning($"[TeamContext:{Team.name}] Tried to remove non-existent modifier {modifier.ID}");
        }
    }

    // ===== 초기화 =====

    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);

        var snapshot = new List<StatModifier>(activeModifiers);
        foreach (var mod in snapshot)
        {
            RemoveModifier(mod);
        }
        activeModifiers.Clear();
    }

    // ===== 최종 스탯 계산 =====

    /// <summary>
    /// 유닛의 기본 능력치에 현재 팀의 버프/디버프를 적용해 최종 값 산출.
    /// </summary>
    public float CalculateStat(Unit unit, StatType statType, float baseValue)
    {
        float finalValue = baseValue;
        float percentAdd = 0f; // 일단 계수는 합연산으로 처리

        foreach (var mod in activeModifiers)
        {
            if (mod.Type != statType || !mod.IsMatch(unit)) continue;

            switch (mod.Operation)
            {
                case ModifierOperation.Override:
                    return mod.Value;

                case ModifierOperation.Add:
                    finalValue += mod.Value;
                    break;

                case ModifierOperation.Multiply:
                    percentAdd += mod.Value;
                    break;
            }
        }

        finalValue *= 1f + percentAdd;

        return Mathf.Max(0, finalValue);
    }
}