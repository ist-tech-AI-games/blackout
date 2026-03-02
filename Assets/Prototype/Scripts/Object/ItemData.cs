using System;
using UnityEngine;

public enum ObjectInteractionOption
{
    None,

    // These two are considered as `All` when the team is not specified or is neutral.
    IgnoreFriend,
    IgnoreEnemy,
    All,
}

/// <summary>
/// Configuration data for item types (batteries, special items).
/// Defines visuals (sprite tiers), effects, max stack size, collision, and interaction rules.
/// </summary>
[CreateAssetMenu(menuName = "Project/Item Data")]
public class ItemData : ScriptableObject
{
    [Serializable]
    public struct ItemAmountTier
    {
        [Tooltip("이 수량 이상일 때 해당 스프라이트 적용")]
        public int MinAmount;
        public Sprite Sprite;
    }

    [Header("Visuals")]
    [SerializeField]
    private Sprite defaultSprite;

    [SerializeField]
    private ItemAmountTier[] amountTiers;

    [field: SerializeField]
    public ItemEffect[] Effects { get; private set; }

    [field: SerializeField]
    public int MaxItemAmount { get; private set; } = 1;

    [field: SerializeField]
    public CollisionBound CollisionBound { get; private set; }

    [field: SerializeField]
    public ObjectInteractionOption InteractionOption { get; private set; } =
        ObjectInteractionOption.IgnoreFriend;

    /// <summary>
    /// Validates configuration values when changed in the Unity Inspector.
    /// Ensures MaxItemAmount is positive and amountTiers are properly configured.
    /// </summary>
    private void OnValidate()
    {
        // MaxItemAmount must be at least 1
        if (MaxItemAmount < 1)
        {
            Debug.LogWarning($"[ItemData:{name}] MaxItemAmount must be at least 1. Resetting to 1.", this);
            MaxItemAmount = 1;
        }

        // Validate amountTiers
        if (amountTiers != null && amountTiers.Length > 0)
        {
            for (int i = 0; i < amountTiers.Length; i++)
            {
                // MinAmount should be non-negative
                if (amountTiers[i].MinAmount < 0)
                {
                    Debug.LogWarning($"[ItemData:{name}] amountTiers[{i}].MinAmount is negative ({amountTiers[i].MinAmount}). Should be >= 0.", this);
                }

                // Warn if tier sprite is missing
                if (amountTiers[i].Sprite == null)
                {
                    Debug.LogWarning($"[ItemData:{name}] amountTiers[{i}].Sprite is null. Consider assigning a sprite or removing this tier.", this);
                }

                // Check if tiers are sorted (recommended for clarity, not required)
                if (i > 0 && amountTiers[i].MinAmount < amountTiers[i - 1].MinAmount)
                {
                    Debug.LogWarning($"[ItemData:{name}] amountTiers are not sorted by MinAmount. Tier {i} ({amountTiers[i].MinAmount}) < Tier {i - 1} ({amountTiers[i - 1].MinAmount}). Consider sorting for clarity.", this);
                }
            }
        }
    }

    public Sprite GetSprite(int amount)
    {
        if (amountTiers == null || amountTiers.Length == 0)
            return defaultSprite;

        Sprite bestMatch = defaultSprite;
        int currentMaxThreshold = -1;

        foreach (var tier in amountTiers)
        {
            if (amount >= tier.MinAmount && tier.MinAmount > currentMaxThreshold)
            {
                currentMaxThreshold = tier.MinAmount;
                bestMatch = tier.Sprite;
            }
        }

        return bestMatch;
    }
}
