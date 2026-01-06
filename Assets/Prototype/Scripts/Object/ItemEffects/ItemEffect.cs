using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public abstract void EnterEffect(TeamContext teamContext, int amount);
    public abstract void ExitEffect(TeamContext teamContext, int amount);
}