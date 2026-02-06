using UnityEngine;

public enum ItemExitReason
{
    Dropped,    // 창고 적재
    Destroyed,  // 유닛 사망, 변신, 리셋 등
    Refresh,    // 재계산
    Absorbed    // 창고 흡수
}

public abstract class ItemEffect : ScriptableObject
{
    public abstract void EnterEffect(TeamContext teamContext, int amount);
    public abstract void ExitEffect(TeamContext teamContext, int amount, ItemExitReason reason);
}