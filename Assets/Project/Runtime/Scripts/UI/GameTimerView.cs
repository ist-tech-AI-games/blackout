using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTimerView : MonoBehaviour
{
    [Header("Episode Timer")]
    [SerializeField] private Image episodeProgressBar;
    [SerializeField] private TextMeshProUGUI episodeTimeText;
    [SerializeField] private string episodePlaceholder = "{0:00}:{1:00}";

    [Header("Absorption Timer")]
    [SerializeField] private Image absorptionFillImage;
    [SerializeField] private TextMeshProUGUI nextAbsorptionText;
    [SerializeField] private string absorptionPlaceholder = "{0:00.0}";

    public void UpdateView(GameTimer episodeTimer, GameTimer absorptionTimer)
    {
        if (episodeTimer != null)
        {
            float remain = episodeTimer.RemainingTime;
            episodeTimeText.SetText(episodePlaceholder, Mathf.FloorToInt(remain / 60), Mathf.FloorToInt(remain % 60));
            
            if (episodeProgressBar != null)
                episodeProgressBar.fillAmount = 1f - episodeTimer.Ratio; 
        }

        if (absorptionTimer != null)
        {
            if (absorptionFillImage != null)
                absorptionFillImage.fillAmount = absorptionTimer.Ratio;

            nextAbsorptionText?.SetText(absorptionPlaceholder, absorptionTimer.RemainingTime);
        }
    }
}