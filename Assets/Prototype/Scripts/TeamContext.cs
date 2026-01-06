public class TeamContext
{
    public TeamData Team { get; private set; }
    public int Score { get; set; }
    
    public TeamContext(TeamData team)
    {
        Team = team;
    }
}