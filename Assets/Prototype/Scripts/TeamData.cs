using UnityEngine;

/// <summary>
/// Configuration data for teams in the game.
/// Defines team name, color, and opponent relationship (must be bidirectional).
/// </summary>
[CreateAssetMenu(fileName="New Team", menuName="Project/Team Data")]
public class TeamData : ScriptableObject
{
    public string TeamName;
    public Color TeamColor;
    [field: SerializeField] public TeamData Opponent { get; private set; }

    /// <summary>
    /// Validates configuration values when changed in the Unity Inspector.
    /// Ensures team name is not empty, opponent is not self, and opponent relationship is symmetric.
    /// </summary>
    private void OnValidate()
    {
        // TeamName should not be empty
        if (string.IsNullOrWhiteSpace(TeamName))
        {
            Debug.LogWarning($"[TeamData:{name}] TeamName is empty. Consider setting a team name.", this);
        }

        // Opponent should not be self
        if (Opponent == this)
        {
            Debug.LogError($"[TeamData:{name}] Opponent is set to self! A team cannot be its own opponent.", this);
        }

        // Check opponent symmetry (if A.Opponent = B, then B.Opponent should = A)
        if (Opponent != null && Opponent.Opponent != this && Opponent.Opponent != null)
        {
            Debug.LogWarning($"[TeamData:{name}] Opponent relationship is not symmetric. This team's opponent is '{Opponent.name}', but that team's opponent is '{Opponent.Opponent.name}' (not this team).", this);
        }
    }

    public bool IsOpponent(TeamData other)
    {
        return other != null && (Opponent == other || other.Opponent == this);
    }
}