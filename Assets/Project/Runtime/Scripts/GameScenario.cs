using UnityEngine;

/// <summary>
/// Represents the current state of the game.
/// </summary>
public enum GameState
{
    /// <summary>Game is being set up, not ready to play yet.</summary>
    Initializing,
    /// <summary>Game is actively running, players can interact.</summary>
    Playing,
    /// <summary>Game has ended, waiting for reset or exit.</summary>
    GameEnded
}

/// <summary>
/// Main game orchestrator that coordinates all managers and controls the game lifecycle.
/// Implements the Facade pattern to provide a single point of control for the entire game.
/// </summary>
public class GameScenario : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private MatchManager gameManager;

    [SerializeField]
    private LevelDirector levelDirector;

    [SerializeField]
    private MapManager mapManager;

    [SerializeField]
    private UIManager uiManager;

    [Header("Settings")]
    [SerializeField]
    private GameBalanceConfig balanceConfig;

    /// <summary>Gets the central event bus for game-wide event communication.</summary>
    public GameEventBus EventBus { get; private set; }

    /// <summary>Gets the manager responsible for all game timers.</summary>
    public TimerManager TimerManager { get; private set; }

    /// <summary>Gets the timer tracking total episode duration (until time expires).</summary>
    public GameTimer EpisodeTimer { get; private set; }

    /// <summary>Gets the repeating timer for storage absorption events.</summary>
    public GameTimer AbsorptionTimer { get; private set; }

    /// <summary>Gets the current game state (Initializing, Playing, or GameEnded).</summary>
    public GameState CurrentState { get; private set; } = GameState.Initializing;

    /// <summary>Gets the active game balance configuration containing all tunable values.</summary>
    public GameBalanceConfig BalanceConfig => balanceConfig;

    /// <summary>Gets the match manager responsible for units, teams, and win conditions.</summary>
    public MatchManager MatchManager => gameManager;

    /// <summary>
    /// Initializes the game scenario and all dependent managers.
    /// Called once at game startup by GameBootstrapper.
    /// </summary>
    public void Initialize()
    {
        EventBus = new GameEventBus();
        TimerManager = new TimerManager();

        gameManager.Initialize(this);
        levelDirector.Initialize(this);

        EventBus.Flow.OnGameEnded += OnGameEnded;
    }

    /// <summary>
    /// Starts a new episode (match/game session).
    /// Resets timers, starts the level, and transitions to Playing state.
    /// Can be called multiple times for episode restarts (e.g., pressing R key).
    /// </summary>
    public void EpisodeBegin()
    {
        CurrentState = GameState.Playing;

        // NOTE: Do NOT call EventBus.Reset() here. Event handlers registered in Initialize()
        // (GameScenario, MatchManager, LevelDirector) must persist across episodes.
        // UIManager handles its own handler de-duplication via unsubscribe-before-subscribe pattern.

        TimerManager.Clear();

        EpisodeTimer = TimerManager.AddTimer(balanceConfig.MaxEpisodeTime, false, () => EventBus.Flow.PublishTimeExpired());
        AbsorptionTimer = TimerManager.AddTimer(balanceConfig.AbsorptionInterval, true, () => EventBus.World.PublishAbsorption());

        levelDirector.StartEpisode();
    }

    /// <summary>
    /// Updates the game state every frame while playing.
    /// Ticks timers, updates level systems, and refreshes UI.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void EpisodeUpdate(float deltaTime)
    {
        if (CurrentState != GameState.Playing) return;
        levelDirector.ManualLateUpdate();
        TimerManager.Tick(deltaTime);
        uiManager?.UpdateUI(EpisodeTimer, AbsorptionTimer);
    }

    /// <summary>
    /// Moves a specific unit based on input.
    /// Called by PlayerUnitController or AI agents.
    /// </summary>
    /// <param name="unitIndex">Index of the unit to move (0-9 for 10 total units).</param>
    /// <param name="moveInput">Normalized 2D movement direction vector.</param>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void MoveUnit(int unitIndex, Vector2 moveInput, float deltaTime)
    {
        if (CurrentState != GameState.Playing) return;
        if (gameManager.Units != null && unitIndex >= 0 && unitIndex < gameManager.Units.Length)
        {
            Unit unit = gameManager.Units[unitIndex];
            if (unit.gameObject.activeInHierarchy)
            {
                unit.Move(moveInput, deltaTime);
            }
        }
    }

    private void OnGameEnded(TeamData winner)
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.GameEnded;
            Debug.Log($"Game Over. Waiting for reset command. Winner: {winner?.name ?? "Draw"}");
        }
    }
}
