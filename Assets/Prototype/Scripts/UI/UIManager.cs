using System;
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

    [SerializeField]
    private GameTimerView timerView;

    [SerializeField]
    private ResultPanelView resultPanelView;

    private MatchManager matchManager;

    public void Initialize(MatchManager matchManager, GameScenario gameScenario)
    {
        this.matchManager = matchManager;
        
        teamAScore?.Bind(matchManager.GetTeamContext(matchManager.TeamA));
        teamBScore?.Bind(matchManager.GetTeamContext(matchManager.TeamB));
        neutralScore?.Bind(matchManager.GetTeamContext(matchManager.NeutralTeam));

        gameScenario.EventBus.Flow.OnGameEnded -= OnGameEnded;
        gameScenario.EventBus.Flow.OnGameEnded += OnGameEnded;

        resultPanelView?.Hide();
    }

    public void UpdateUI(GameTimer episodeTimer, GameTimer absorptionTimer)
    {
        timerView.UpdateView(episodeTimer, absorptionTimer);
    }

    private void OnGameEnded(TeamData winner)
    {
        if (resultPanelView == null || matchManager == null) return;

        int scoreA = matchManager.GetTeamContext(matchManager.TeamA).Score;
        int scoreB = matchManager.GetTeamContext(matchManager.TeamB).Score;

        resultPanelView.Show(winner, scoreA, scoreB);
    }
}
