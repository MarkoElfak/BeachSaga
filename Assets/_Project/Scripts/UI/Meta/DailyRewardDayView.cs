using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>One tile of the daily reward calendar: "Day 3", the reward, and a colour for its state.</summary>
    public sealed class DailyRewardDayView : MonoBehaviour
    {
        [SerializeField] private Image background = null;
        [SerializeField] private TMP_Text dayLabel = null;
        [SerializeField] private TMP_Text rewardLabel = null;

        public void Show(string day, string reward, Color color)
        {
            gameObject.SetActive(true);
            dayLabel.text = day;
            rewardLabel.text = reward;
            background.color = color;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
