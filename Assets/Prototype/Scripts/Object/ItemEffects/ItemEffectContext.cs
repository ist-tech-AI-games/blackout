using System;
using System.Collections.Generic;

/// <summary>
/// Context object providing all dependencies and services that ItemEffects need to apply their logic.
/// This enables proper dependency injection and decouples effects from ItemObject internals.
/// </summary>
public class ItemEffectContext
{
    /// <summary>The team context that owns the storage where this item resides.</summary>
    public TeamContext OwnerContext { get; }

    /// <summary>The amount of the item (e.g., battery amount = score value).</summary>
    public int ItemAmount { get; }

    private ItemExitReason ExitReasonInternal;

    /// <summary>Why the item is exiting the storage (for exit logic only).</summary>
    public ItemExitReason ExitReason => ExitReasonInternal;

    /// <summary>Reference to the MatchManager for resolving team contexts.</summary>
    public MatchManager MatchManager { get; }

    /// <summary>The item object that this effect belongs to (for tracking modifiers).</summary>
    public object EffectSource { get; }

    // Callbacks for modifier management (used by BuffItemEffect)
    private readonly Action<TeamContext, StatModifier> addModifierCallback;
    private readonly Action<TeamContext, StatModifier> removeModifierCallback;

    /// <summary>
    /// Creates a context for entering an effect (when item is placed in storage).
    /// </summary>
    public ItemEffectContext(
        TeamContext ownerContext,
        int itemAmount,
        MatchManager matchManager,
        object effectSource,
        Action<TeamContext, StatModifier> addModifier,
        Action<TeamContext, StatModifier> removeModifier)
    {
        OwnerContext = ownerContext;
        ItemAmount = itemAmount;
        ExitReasonInternal = ItemExitReason.Dropped; // Default, overridden for exit
        MatchManager = matchManager;
        EffectSource = effectSource;
        addModifierCallback = addModifier;
        removeModifierCallback = removeModifier;
    }

    /// <summary>
    /// Creates a context for exiting an effect (when item leaves storage).
    /// </summary>
    public static ItemEffectContext CreateForExit(
        TeamContext ownerContext,
        int itemAmount,
        ItemExitReason exitReason,
        MatchManager matchManager,
        object effectSource,
        Action<TeamContext, StatModifier> addModifier,
        Action<TeamContext, StatModifier> removeModifier)
    {
        var ctx = new ItemEffectContext(ownerContext, itemAmount, matchManager, effectSource, addModifier, removeModifier);
        ctx.ExitReasonInternal = exitReason;
        return ctx;
    }

    /// <summary>
    /// Adds a stat modifier to a target team context.
    /// Used by BuffItemEffect to apply buffs/debuffs.
    /// </summary>
    public void AddModifier(TeamContext targetContext, StatModifier modifier)
    {
        addModifierCallback?.Invoke(targetContext, modifier);
    }

    /// <summary>
    /// Removes a stat modifier from a target team context.
    /// Used by BuffItemEffect to remove buffs/debuffs.
    /// </summary>
    public void RemoveModifier(TeamContext targetContext, StatModifier modifier)
    {
        removeModifierCallback?.Invoke(targetContext, modifier);
    }

    /// <summary>
    /// Resolves which team contexts should be affected based on target strategy.
    /// Used by BuffItemEffect to determine buff/debuff targets.
    /// </summary>
    public List<TeamContext> ResolveTargetContexts(EffectTargetStrategy strategy)
    {
        var results = new List<TeamContext>();
        if (MatchManager == null || OwnerContext == null) return results;

        switch (strategy)
        {
            case EffectTargetStrategy.OwnerTeam:
                results.Add(OwnerContext);
                break;

            case EffectTargetStrategy.OpponentTeam:
                if (OwnerContext.Team != null)
                {
                    var enemyData = MatchManager.OpponentTeam(OwnerContext.Team);
                    if (enemyData != null)
                    {
                        var enemyCtx = MatchManager.GetTeamContext(enemyData);
                        if (enemyCtx != null) results.Add(enemyCtx);
                    }
                }
                break;

            case EffectTargetStrategy.AllTeams:
                results.AddRange(MatchManager.GetPlayableTeamContexts());
                break;
        }

        return results;
    }
}
