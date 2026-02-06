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

    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    public void SetScore(int newValue, bool notify = true)
    {
        if (Score == newValue) return;

        Score = newValue;
        if (notify)
            OnScoreChanged?.Invoke(Score);
    }

    public void AddScore(int amount, bool notify = true) => SetScore(Score + amount, notify);
}