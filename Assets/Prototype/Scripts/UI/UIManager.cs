using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Logic")]
    [SerializeField] private LevelDirector levelDirector;
    [Header("Score Views")]
    [SerializeField] private TeamScoreView teamAScore;
    [SerializeField] private TeamScoreView teamBScore;
    [SerializeField] private TeamScoreView neutralScore;

    void OnEnable()
    {
        levelDirector.OnLevelInitialized += Initialize;
    }

    void OnDisable()
    {
        levelDirector.OnLevelInitialized -= Initialize;
    }

    private void Initialize(GameManager gameManager)
    {
        teamAScore?.Bind(gameManager.GetTeamContext(gameManager.TeamA));
        teamBScore?.Bind(gameManager.GetTeamContext(gameManager.TeamB));
        neutralScore?.Bind(gameManager.GetTeamContext(gameManager.NeutralTeam));
    }
}