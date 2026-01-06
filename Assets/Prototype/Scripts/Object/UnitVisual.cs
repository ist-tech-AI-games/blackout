using UnityEngine;

public class UnitVisual : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer.sprite = unit.UnitData.Sprite;
        spriteRenderer.color = unit.Team.TeamColor;
    }

    void OnEnable()
    {
        unit.OnClassChanged += OnUnitClassChanged;
    }

    void OnDisable()
    {
        unit.OnClassChanged -= OnUnitClassChanged;
    }

    private void OnUnitClassChanged(UnitData unitData)
    {
        spriteRenderer.sprite = unitData.Sprite;
    }
}
