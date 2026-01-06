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
}
