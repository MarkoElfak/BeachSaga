using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>"While you were away" popup: collect the offline coins, or watch a rewarded ad for double.</summary>
    public sealed class OfflineEarningsPopupView : MonoBehaviour
    {
        private const int SecondsPerHour = 3600;
        private const int SecondsPerMinute = 60;

        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text body = null;
        [SerializeField] private Button collectButton = null;
        [SerializeField] private Button doubleButton = null;
        [SerializeField] private TMP_Text doubleLabel = null;

        private OfflineEarningsService _offline;
        private IAdsService _ads;

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _offline))
            {
                return;
            }

            _ads = ServiceLocator.Get<IAdsService>();
            collectButton.onClick.AddListener(OnCollect);
            doubleButton.onClick.AddListener(OnDouble);
            EventBus.Subscribe<OfflineRewardClaimed>(OnClaimed);
            if (_offline.HasPending)
            {
                Open();
            }
        }

        private void OnDestroy()
        {
            collectButton.onClick.RemoveListener(OnCollect);
            doubleButton.onClick.RemoveListener(OnDouble);
            EventBus.Unsubscribe<OfflineRewardClaimed>(OnClaimed);
        }

        private void Open()
        {
            int seconds = Mathf.RoundToInt(_offline.PendingSeconds);
            int hours = seconds / SecondsPerHour;
            int minutes = seconds % SecondsPerHour / SecondsPerMinute;
            string away = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
            body.text = $"Your beach kept working for {away}.\nYou earned <b>{NumberFormatter.Format(_offline.PendingCoins)}</b> coins.";
            doubleLabel.text = $"Watch ad: {NumberFormatter.Format(_offline.DoubledCoins)}";
            doubleButton.gameObject.SetActive(_ads.IsRewardedReady);
            root.SetActive(true);
        }

        private void OnCollect() => _offline.Claim();

        private void OnDouble()
        {
            doubleButton.interactable = false;
            _offline.ClaimWithAd();
        }

        private void OnClaimed(OfflineRewardClaimed evt) => root.SetActive(false);
    }
}
