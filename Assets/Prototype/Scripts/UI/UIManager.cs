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

    [Header("Timer Views")]
    [SerializeField]
    private GameTimerView timerView;

    [Header("Effect Views")]
    [SerializeField] private ActiveEffectsView teamAEffects;
    [SerializeField] private ActiveEffectsView teamBEffects;
    [SerializeField] private EffectTooltipView tooltipView;

    [Header("Result Views")]
    [SerializeField]
    private ResultPanelView resultPanelView;

    private MatchManager matchManager;

    public void Initialize(MatchManager matchManager, GameScenario gameScenario)
    {
        this.matchManager = matchManager;
        
        teamAScore?.Bind(matchManager.GetTeamContext(matchManager.TeamA));
        teamBScore?.Bind(matchManager.GetTeamContext(matchManager.TeamB));
        neutralScore?.Bind(matchManager.GetTeamContext(matchManager.NeutralTeam));

        teamAEffects?.Bind(matchManager.GetTeamContext(matchManager.TeamA), tooltipView);
        teamBEffects?.Bind(matchManager.GetTeamContext(matchManager.TeamB), tooltipView);

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
