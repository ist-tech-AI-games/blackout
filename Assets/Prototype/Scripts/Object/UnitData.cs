using UnityEngine;

/// <summary>
/// Configuration data for unit types (Worker, Guard, Carrier).
/// Defines sprite, collision, speed, collectability, and combat relationships.
/// </summary>
[CreateAssetMenu(fileName = "New UnitData", menuName = "Project/Unit Data")]
public class UnitData : ScriptableObject
{
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public CollisionBound CollisionBound { get; private set; }
    [field: SerializeField] public float BaseSpeed { get; private set; }
    [field: SerializeField] public bool Collectable { get; private set; }
    [field: SerializeField] public UnitData[] Beats { get; private set; }

    /// <summary>
    /// Validates configuration values when changed in the Unity Inspector.
    /// Ensures BaseSpeed is positive and Beats array doesn't contain null.
    /// </summary>
    private void OnValidate()
    {
        // BaseSpeed must be positive
        if (BaseSpeed <= 0f)
        {
            Debug.LogWarning($"[UnitData:{name}] BaseSpeed must be positive. Resetting to 1.0f", this);
            BaseSpeed = 1.0f;
        }

        // Beats array validation
        if (Beats != null)
        {
            for (int i = 0; i < Beats.Length; i++)
            {
                if (Beats[i] == null)
                {
                    Debug.LogWarning($"[UnitData:{name}] Beats array contains null at index {i}. Consider removing empty entries.", this);
                }
            }
        }
    }
}