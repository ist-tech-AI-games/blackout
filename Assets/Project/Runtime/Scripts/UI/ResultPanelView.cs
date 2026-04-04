using TMPro;
using UnityEngine;

public class ResultPanelView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI scoreInfoText;

    [Header("Message Formats")]
    [SerializeField] private string winnerPlaceholder = "{0} Wins!";
    [SerializeField] private string scorePlaceholder = "Final Score\nTeam A: {0}  vs  Team B: {1}";
    [SerializeField] private string drawText = "Draw!";
    [SerializeField] private Color drawColor = Color.gray;

    public void Show(TeamData winner, int scoreA, int scoreB)
    {
        panelRoot.SetActive(true);

        if (winner != null)
        {
            winnerText.text = string.Format(winnerPlaceholder, winner.name);
            winnerText.color = winner.TeamColor;
        }
        else
        {
            winnerText.text = drawText;
            winnerText.color = drawColor;
        }

        scoreInfoText.text = string.Format(scorePlaceholder, scoreA, scoreB);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}