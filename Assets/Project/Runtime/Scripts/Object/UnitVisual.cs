using UnityEngine;

public class UnitVisual : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer.sprite = unit.UnitData.Sprite;
        UpdateVisualScale();
    }

    void OnEnable()
    {
        unit.OnClassChanged += OnUnitClassChanged;
        unit.OnStatsChanged += OnUnitStatsChanged;
    }

    void OnDisable()
    {
        unit.OnClassChanged -= OnUnitClassChanged;
        unit.OnStatsChanged -= OnUnitStatsChanged;
    }

    private void OnUnitClassChanged(UnitData unitData)
    {
        spriteRenderer.sprite = UnitVisualManager.Instance.GetArt(unitData, unit.Team);
    }

    private void OnUnitStatsChanged()
    {
        UpdateVisualScale();
    }

    private void UpdateVisualScale()
    {
        if (unit != null)
        {
            unit.transform.localScale = Vector3.one * unit.UnitSize;
        }
    }
}
