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
    public ItemEffect Effect { get; private set; }

    [field: SerializeField]
    public int MaxItemAmount { get; private set; } = 1;

    [field: SerializeField]
    public CollisionBound CollisionBound { get; private set; }

    [field: SerializeField]
    public ObjectInteractionOption InteractionOption { get; private set; } =
        ObjectInteractionOption.IgnoreFriend;

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
