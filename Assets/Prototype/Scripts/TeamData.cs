using UnityEngine;

[CreateAssetMenu(fileName="New Team", menuName="Project/Team Data")]
public class TeamData : ScriptableObject
{
    public string TeamName;
    public Color TeamColor;
    [field: SerializeField] public TeamData Opponent { get; private set; }

    public bool IsOpponent(TeamData other)
    {
        return other != null && (Opponent == other || other.Opponent == this);
    }
}