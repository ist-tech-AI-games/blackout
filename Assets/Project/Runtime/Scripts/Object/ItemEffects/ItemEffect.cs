using UnityEngine;

public enum ItemExitReason
{
    Dropped,    // 창고 적재
    Destroyed,  // 유닛 사망, 변신, 리셋 등
    Refresh,    // 재계산
    Absorbed    // 창고 흡수
}

/// <summary>
/// Base class for all item effects that are applied when items enter/exit storage.
/// Uses dependency injection via ItemEffectContext to enable proper polymorphism.
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    /// <summary>
    /// Called when an item enters a storage region.
    /// Implementations should use the context to apply their specific effects.
    /// </summary>
    /// <param name="context">Complete context with all dependencies needed for the effect.</param>
    public abstract void OnEnterStorage(ItemEffectContext context);

    /// <summary>
    /// Called when an item exits a storage region.
    /// Implementations should use the context (including ExitReason) to handle cleanup.
    /// </summary>
    /// <param name="context">Complete context with all dependencies and exit reason.</param>
    public abstract void OnExitStorage(ItemEffectContext context);
}