using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reward values for ML training, loaded from StreamingAssets/reward_config.json.
/// Item rewards are keyed by ItemData asset name.
/// </summary>
[Serializable]
public class RewardConfig
{
    [Serializable]
    public struct ItemRewardEntry
    {
        public string itemName;
        public float value;
    }

    public ItemRewardEntry[] itemRewards = Array.Empty<ItemRewardEntry>();
    public float killReward = 0.3f;
    public float deathPenalty = 0.2f;
    public float teamScoreReward = 0.1f;
    public float teamScorePenalty = 0.1f;

    private Dictionary<string, float> _map;

    public float GetItemReward(string itemName, float fallback = 0.1f)
    {
        if (_map == null) BuildMap();
        return _map.TryGetValue(itemName, out float v) ? v : fallback;
    }

    private void BuildMap()
    {
        _map = new Dictionary<string, float>();
        if (itemRewards == null) return;
        foreach (var entry in itemRewards)
            _map[entry.itemName] = entry.value;
    }

    public static RewardConfig Load()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "reward_config.json");
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"[RewardConfig] {path} not found. Using defaults.");
            return new RewardConfig();
        }
        RewardConfig config = JsonUtility.FromJson<RewardConfig>(System.IO.File.ReadAllText(path));
        Debug.Log($"[RewardConfig] Loaded from {path}");
        return config;
    }
}
