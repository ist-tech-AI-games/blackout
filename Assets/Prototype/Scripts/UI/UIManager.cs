using UnityEngine;

public class UIManager : MonoBehaviour
{

    [Header("Score Views")]
    [SerializeField]
    private TeamScoreView teamAScore;

    [SerializeField]
    private TeamScoreView teamBScore;

    [SerializeField]
    private TeamScoreView neutralScore;

    public void Initialize(MatchManager matchManager)
    {
        teamAScore?.Bind(matchManager.GetTeamContext(matchManager.TeamA));
        teamBScore?.Bind(matchManager.GetTeamContext(matchManager.TeamB));
        neutralScore?.Bind(matchManager.GetTeamContext(matchManager.NeutralTeam));
    }
}
