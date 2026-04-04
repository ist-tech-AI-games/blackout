using UnityEngine;

/// <summary>
/// Item effect that adds or removes score from the owning team's context.
/// Score is added when entering storage and removed (conditionally) when exiting.
/// </summary>
[CreateAssetMenu(menuName = "Project/Item Effects/Score Item Effect")]
public class ScoreItemEffect : ItemEffect
{
    public override void OnEnterStorage(ItemEffectContext context)
    {
        context.OwnerContext.AddScore(context.ItemAmount, notify: true);
    }

    public override void OnExitStorage(ItemEffectContext context)
    {
        switch (context.ExitReason)
        {
            case ItemExitReason.Absorbed:
                // Score is permanently locked when absorbed, don't remove
                break;
            case ItemExitReason.Refresh:
                // Silent recalculation, remove score without notification
                context.OwnerContext.AddScore(-context.ItemAmount, notify: false);
                break;
            case ItemExitReason.Destroyed:
            case ItemExitReason.Dropped:
            default:
                // Item was removed from storage, deduct score with notification
                context.OwnerContext.AddScore(-context.ItemAmount, notify: true);
                break;
        }
    }
}