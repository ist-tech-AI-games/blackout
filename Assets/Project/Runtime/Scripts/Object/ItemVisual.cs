using TMPro;
using UnityEngine;

public class ItemVisual : MonoBehaviour
{
    [SerializeField] private ItemObject itemObject;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro amountText;
    [SerializeField] private string placeholder = "{0}";

    void OnEnable()
    {
        itemObject ??= GetComponent<ItemObject>();
        itemObject.OnDataUpdated += UpdateVisual;
        UpdateVisual(itemObject.ItemData, itemObject.ItemAmount);
    }

    void OnDisable()
    {
        itemObject.OnDataUpdated -= UpdateVisual;
    }

    public void UpdateVisual(ItemData itemData, int amount)
    {
        spriteRenderer.sprite = itemData.GetSprite(amount);
        amountText.SetText(amount > 1 ? placeholder : string.Empty, amount);
    }
}