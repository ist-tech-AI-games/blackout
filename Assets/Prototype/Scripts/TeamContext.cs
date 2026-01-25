using System;

public class TeamContext
{
    public event Action<int> OnScoreChanged;
    public TeamData Team { get; private set; }
    public int Score { get; private set; } = 0;
    
    public TeamContext(TeamData team)
    {
        Team = team;
    }

    public void SetScore(int newValue)
    {
        Score = newValue;
        OnScoreChanged?.Invoke(Score);
    }

    public void AddScore(int amount) => SetScore(Score + amount);
}