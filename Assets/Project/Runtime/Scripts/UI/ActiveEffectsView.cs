using System.Collections.Generic;
using UnityEngine;

public class ActiveEffectsView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform iconRoot;
    [SerializeField] private EffectIconView iconPrefab;

    private Dictionary<StatModifier, EffectIconView> activeIcons = new();
    private TeamContext boundContext;
    private EffectTooltipView effectTooltipView;

    public void Bind(TeamContext context, EffectTooltipView tooltip)
    {
        effectTooltipView = tooltip;
        // 기존 연결 해제
        if (boundContext != null)
        {
            boundContext.OnModifierAdded -= HandleModifierAdded;
            boundContext.OnModifierRemoved -= HandleModifierRemoved;
            ClearAllIcons();
        }

        boundContext = context;

        if (boundContext != null)
        {
            boundContext.OnModifierAdded += HandleModifierAdded;
            boundContext.OnModifierRemoved += HandleModifierRemoved;
        }
    }

    private void HandleModifierAdded(StatModifier mod)
    {
        if (mod.DisplayInfo.IsHidden || activeIcons.ContainsKey(mod)) return;

        // 나중에 여유 되고 문제 생기면 Pooling으로...
        EffectIconView newIcon = Instantiate(iconPrefab, iconRoot);
        newIcon.SetData(mod.DisplayInfo);

        if (effectTooltipView != null)
        {
            newIcon.OnHoverEnter += effectTooltipView.Show;
            newIcon.OnHoverExit += effectTooltipView.Hide;
        }

        activeIcons.Add(mod, newIcon);
    }

    private void HandleModifierRemoved(StatModifier mod)
    {
        if (activeIcons.TryGetValue(mod, out var icon))
        {
            if (effectTooltipView != null)
            {
                icon.OnHoverEnter -= effectTooltipView.Show;
                icon.OnHoverExit -= effectTooltipView.Hide;
            }

            Destroy(icon.gameObject);
            activeIcons.Remove(mod);
        }
    }

    private void ClearAllIcons()
    {
        foreach (var icon in activeIcons.Values)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        activeIcons.Clear();
    }

    private void OnDestroy()
    {
        if (boundContext != null)
        {
            boundContext.OnModifierAdded -= HandleModifierAdded;
            boundContext.OnModifierRemoved -= HandleModifierRemoved;
        }
    }
}