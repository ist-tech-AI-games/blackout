using UnityEngine;

[CreateAssetMenu(menuName = "Project/Item Effects/Score Item Effect")]
public class ScoreItemEffect : ItemEffect
{
    public override void EnterEffect(TeamContext teamContext, int amount)
    {
        teamContext.AddScore(amount);
    }

    public override void ExitEffect(TeamContext teamContext, int amount)
    {
        teamContext.AddScore(-amount);
    }
}