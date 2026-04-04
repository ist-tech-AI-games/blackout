using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectTooltipView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Show(EffectDisplayInfo info)
    {
        panelRoot.SetActive(true);
        
        if (iconImage != null)
            iconImage.sprite = info.Icon;
        nameText?.SetText(info.Name);
        descriptionText?.SetText(info.Description);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}