using TMPro;
using UnityEngine;

public class TeamScoreView : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TeamData teamData;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string placeholder = "{0}";
    private TeamContext teamContext;

    void Awake()
    {
        gameManager ??= FindAnyObjectByType<GameManager>();
    }

    // 일단은 여기서 초기화. 이후 문제가 생기면 초기화 절차를 중앙화하도록 리펙토링.
    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        teamContext = gameManager.GetTeamContext(teamData);
        UpdateScoreText(teamContext.Score);
        teamContext.OnScoreChanged += UpdateScoreText;
    }

    public void UpdateScoreText(int score) => scoreText.SetText(placeholder, score);
}