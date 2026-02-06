using UnityEngine;

public enum GameState
{
    Initializing,
    Playing,
    GameEnded
}

public class GameScenario : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private MatchManager gameManager;

    [SerializeField]
    private LevelDirector levelDirector;

    [SerializeField]
    private MapManager mapManager;

    [Header("Settings")]
    [SerializeField]
    private float maxEpisodeTime = 600f;

    [SerializeField]
    private float absorptionInterval = 120f;

    public GameEventBus EventBus { get; private set; }
    public TimerManager TimerManager { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Initializing;

    public void Initialize()
    {
        EventBus = new GameEventBus();
        TimerManager = new TimerManager();

        gameManager.Initialize(this);
        levelDirector.Initialize(this);

        EventBus.Flow.OnGameEnded += OnGameEnded;
    }

    public void EpisodeBegin()
    {
        CurrentState = GameState.Playing;
        // EventBus.Reset();
        TimerManager.Clear();

        TimerManager.AddTimer(maxEpisodeTime, false, () => EventBus.Flow.PublishTimeExpired());
        TimerManager.AddTimer(absorptionInterval, true, () => EventBus.World.PublishAbsorption());

        levelDirector.StartEpisode();
    }

    public void EpisodeUpdate(float deltaTime)
    {
        if (CurrentState != GameState.Playing) return;
        TimerManager.Tick(deltaTime);
    }

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
