using UnityEngine;

[CreateAssetMenu(fileName = "New UnitData", menuName = "Project/Unit Data")]
public class UnitData : ScriptableObject
{
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public CollisionBound CollisionBound { get; private set; }
    [field: SerializeField] public float BaseSpeed { get; private set; }
    [field: SerializeField] public bool Collectable { get; private set; }
    [field: SerializeField] public UnitData[] Beats { get; private set; }
}