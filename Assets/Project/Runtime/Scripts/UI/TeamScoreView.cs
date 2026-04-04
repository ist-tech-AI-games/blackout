using TMPro;
using UnityEngine;

public class TeamScoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string placeholder = "{0}";
    
    private TeamContext boundContext;

    public void Bind(TeamContext context)
    {
        Unbind();

        boundContext = context;

        if (boundContext != null)
        {
            UpdateScoreText(boundContext.Score);
            boundContext.OnScoreChanged += UpdateScoreText;
        }
    }

    private void Unbind()
    {
        if (boundContext != null)
        {
            boundContext.OnScoreChanged -= UpdateScoreText;
            boundContext = null;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void UpdateScoreText(int score)
    {
        scoreText.SetText(placeholder, score);
    }
}