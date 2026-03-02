using System;
using UnityEngine;

/// <summary>
/// Manages all UI views and binds them to game data.
/// Subscribes to GameEventBus for decoupled initialization (no direct reference from LevelDirector).
/// </summary>
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

    [Header("Dependencies")]
    [SerializeField]
    private GameScenario gameScenario;

    private MatchManager matchManager;

    /// <summary>
    /// Subscribe to episode started event during Unity Awake.
    /// This decouples UI from logic - LevelDirector doesn't need to reference UIManager.
    /// </summary>
    private void Awake()
    {
        if (gameScenario != null)
        {
            // Unsubscribe first to prevent duplicate subscriptions
            gameScenario.EventBus.Flow.OnEpisodeStarted -= OnEpisodeStarted;
            gameScenario.EventBus.Flow.OnEpisodeStarted += OnEpisodeStarted;
        }
    }

    /// <summary>
    /// Called when episode starts via GameEventBus event.
    /// Binds all UI views to match data.
    /// </summary>
    private void OnEpisodeStarted(MatchManager matchManager, GameScenario gameScenario)
    {
        Initialize(matchManager, gameScenario);
    }

    /// <summary>
    /// Initializes UI by binding views to match data.
    /// Now called via event instead of direct reference from LevelDirector.
    /// </summary>
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

    /// <summary>
    /// Unity lifecycle method - cleanup event subscriptions to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (gameScenario != null)
        {
            gameScenario.EventBus.Flow.OnEpisodeStarted -= OnEpisodeStarted;
        }
    }
}
