using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EffectIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    
    private EffectDisplayInfo effectInfo;

    public event Action<EffectDisplayInfo> OnHoverEnter;
    public event Action OnHoverExit;

    public void SetData(EffectDisplayInfo info)
    {
        effectInfo = info;

        if (iconImage != null)
        {
            if (info.Icon != null)
            {
                iconImage.sprite = info.Icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter?.Invoke(effectInfo);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke();
    }

    void OnDisable()
    {
        OnHoverExit?.Invoke();
    }
}