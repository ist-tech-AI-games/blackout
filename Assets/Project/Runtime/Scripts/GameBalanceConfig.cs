using UnityEngine;

/// <summary>
/// Centralized configuration for all game balance and tunable values.
/// This ScriptableObject provides a single source of truth for game parameters,
/// making it easy to balance the game and create multiple difficulty profiles.
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Project/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Match Timing")]
    [Tooltip("Maximum episode duration in seconds before time expires")]
    public float MaxEpisodeTime = 600f;

    [Tooltip("Interval between absorption events in seconds (when storages flush items)")]
    public float AbsorptionInterval = 120f;

    [Header("Win Conditions")]
    [Tooltip("Score required to win the match immediately")]
    public int TargetScore = 100;

    [Header("Map Generation")]
    [Tooltip("Total score value of batteries spawned at map generation (approximately)")]
    public int InitialBatteryTotalScore = 150;

    [Tooltip("Minimum battery amount when spawning. Must not exceed ItemData.MaxItemAmount (10 for batteries).")]
    [Range(1, 10)]
    public int MinBatteryAmount = 1;

    [Tooltip("Maximum battery amount when spawning. Must not exceed ItemData.MaxItemAmount (10 for batteries).")]
    [Range(1, 10)]
    public int MaxBatteryAmount = 5;

    [Tooltip("Number of storage regions per team (excluding the protected base storage)")]
    [Range(1, 5)]
    public int StorageCountPerTeam = 3;

    [Tooltip("Generate batteries symmetrically across the map (y=x diagonal mirror)")]
    public bool SymmetricBatterySpawn = true;

    [Header("Dynamic Item Spawning")]
    [Tooltip("Cooldown between special item spawns in seconds")]
    public float SpecialItemSpawnCooldown = 15f;

    [Header("Object Pooling")]
    [Tooltip("Initial size of the item object pool")]
    [Range(10, 100)]
    public int ItemPoolInitialSize = 30;

    [Tooltip("Maximum size of the item object pool")]
    [Range(50, 200)]
    public int ItemPoolMaxSize = 150;

    [Header("Debug & Development")]
    [Tooltip("Enable detailed logging for debugging gameplay systems (currently not used)")]
    public bool VerboseLogging = false;

    [Tooltip("Show debug visualizations in the Scene view (Currently not used)")]
    public bool ShowDebugGizmos = false;

    /// <summary>
    /// Validates configuration values to ensure they are within acceptable ranges.
    /// Called automatically by Unity when values are changed in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        // Timing validation
        MaxEpisodeTime = Mathf.Max(1f, MaxEpisodeTime);
        AbsorptionInterval = Mathf.Max(1f, AbsorptionInterval);

        // Win condition validation
        TargetScore = Mathf.Max(1, TargetScore);

        // Map generation validation
        InitialBatteryTotalScore = Mathf.Max(1, InitialBatteryTotalScore);
        MinBatteryAmount = Mathf.Clamp(MinBatteryAmount, 1, 10);
        MaxBatteryAmount = Mathf.Clamp(MaxBatteryAmount, MinBatteryAmount, 10);

        // Pool validation
        ItemPoolInitialSize = Mathf.Max(1, ItemPoolInitialSize);
        ItemPoolMaxSize = Mathf.Max(ItemPoolInitialSize, ItemPoolMaxSize);

        // Timing logic validation
        if (AbsorptionInterval >= MaxEpisodeTime)
        {
            Debug.LogWarning($"[GameBalanceConfig] AbsorptionInterval ({AbsorptionInterval}s) should be less than MaxEpisodeTime ({MaxEpisodeTime}s) to trigger at least once per match.");
        }
    }

    /// <summary>
    /// Returns a descriptive summary of the current balance configuration.
    /// Useful for debugging and understanding which profile is active.
    /// </summary>
    public string GetConfigSummary()
    {
        return $"Match Duration: {MaxEpisodeTime}s | " +
               $"Target Score: {TargetScore} | " +
               $"Absorption Interval: {AbsorptionInterval}s | " +
               $"Initial Batteries: ~{InitialBatteryTotalScore} points";
    }
}
