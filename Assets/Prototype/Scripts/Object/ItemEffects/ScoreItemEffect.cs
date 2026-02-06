using UnityEngine;

[CreateAssetMenu(menuName = "Project/Item Effects/Score Item Effect")]
public class ScoreItemEffect : ItemEffect
{
    public override void EnterEffect(TeamContext teamContext, int amount)
    {
        teamContext.AddScore(amount, notify: true);
    }

    public override void ExitEffect(TeamContext teamContext, int amount, ItemExitReason reason)
    {
        switch (reason)
        {
            case ItemExitReason.Absorbed:
                break;
            case ItemExitReason.Refresh:
                teamContext.AddScore(-amount, notify: false);
                break;
            case ItemExitReason.Destroyed:
            case ItemExitReason.Dropped:
            default:
                teamContext.AddScore(-amount, notify: true);
                break;

        }
    }
}