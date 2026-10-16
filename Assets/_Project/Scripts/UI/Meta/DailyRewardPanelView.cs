using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Daily login calendar. Opens by itself once per session when a reward is waiting (after the tutorial
    /// and the offline popup), and from the HUD button, whose badge shows while today's reward is unclaimed.
    /// </summary>
    public sealed class DailyRewardPanelView : MonoBehaviour
    {
        private const float RefreshSeconds = 1f;

        [SerializeField] private Button openButton = null;
        [SerializeField] private GameObject badge = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private DailyRewardDayView[] days = null;
        [SerializeField] private TMP_Text body = null;
        [SerializeField] private Button collectButton = null;
        [SerializeField] private TMP_Text collectLabel = null;
        [SerializeField] private Button doubleButton = null;
        [SerializeField] private TMP_Text doubleLabel = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Color claimedColor = new Color(0.62f, 0.85f, 0.6f);
        [SerializeField] private Color todayColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color upcomingColor = new Color(0.86f, 0.9f, 0.95f);

        private DailyRewardService _daily;
        private OfflineEarningsService _offline;
        private TutorialService _tutorial;
        private IAdsService _ads;
        private float _refreshTimer;

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _daily))
            {
                openButton.gameObject.SetActive(false);
                return;
            }

            _offline = ServiceLocator.Get<OfflineEarningsService>();
            _tutorial = ServiceLocator.Get<TutorialService>();
            _ads = ServiceLocator.Get<IAdsService>();
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            collectButton.onClick.AddListener(OnCollect);
            doubleButton.onClick.AddListener(OnDouble);
            EventBus.Subscribe<DailyRewardClaimed>(OnClaimed);
            EventBus.Subscribe<OfflineRewardClaimed>(OnOfflineClaimed);
            RefreshButton();
            TryAutoOpen();
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            collectButton.onClick.RemoveListener(OnCollect);
            doubleButton.onClick.RemoveListener(OnDouble);
            EventBus.Unsubscribe<DailyRewardClaimed>(OnClaimed);
            EventBus.Unsubscribe<OfflineRewardClaimed>(OnOfflineClaimed);
        }

        // Cheap poll so the badge appears at local midnight and the button once the tutorial ends.
        private void Update()
        {
            if (_daily == null)
            {
                return;
            }

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f)
            {
                return;
            }

            _refreshTimer = RefreshSeconds;
            RefreshButton();
        }

        public void Open()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            root.SetActive(true);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        // The tutorial and the offline popup come first; the calendar waits for them.
        private void TryAutoOpen()
        {
            if (_daily.AutoOpenedThisSession || !_daily.OpenOnLaunch || !_daily.CanClaim || _tutorial.IsActive || _offline.HasPending)
            {
                return;
            }

            _daily.AutoOpenedThisSession = true;
            root.SetActive(true);
            Refresh();
        }

        private void RefreshButton()
        {
            bool available = !_tutorial.IsActive;
            if (openButton.gameObject.activeSelf != available) openButton.gameObject.SetActive(available);
            bool waiting = available && _daily.CanClaim;
            if (badge.activeSelf != waiting) badge.SetActive(waiting);
        }

        private void Refresh()
        {
            int today = _daily.TodayIndex;
            bool canClaim = _daily.CanClaim;
            for (int i = 0; i < days.Length; i++)
            {
                if (i >= _daily.CycleLength)
                {
                    days[i].Hide();
                    continue;
                }

                bool claimed = _daily.IsClaimed(i);
                Color color = claimed ? claimedColor : i == today ? todayColor : upcomingColor;
                days[i].Show($"Day {i + 1}", claimed ? "Done" : RewardText(i), color);
            }

            int streak = _daily.Streak;
            if (canClaim)
            {
                body.text = _daily.ContinuesStreak
                    ? $"{streak}-day streak! Keep it going."
                    : $"Come back every day for bigger rewards. Day {_daily.CycleLength} is the jackpot!";
            }
            else
            {
                body.text = $"{streak}-day streak. Come back tomorrow for day {(today + 1) % _daily.CycleLength + 1}!";
            }

            collectButton.gameObject.SetActive(canClaim);
            collectButton.interactable = true;
            collectLabel.text = $"Collect {NumberFormatter.Format(_daily.TodayCoins)}";
            doubleButton.gameObject.SetActive(canClaim && _ads.IsRewardedReady);
            doubleButton.interactable = true;
            doubleLabel.text = $"{AdWording.Watch(_ads)}: {NumberFormatter.Format(_daily.DoubledCoins)}";
        }

        private string RewardText(int dayIndex)
        {
            string coins = NumberFormatter.Format(_daily.CoinsFor(dayIndex));
            float boost = _daily.BoostMinutesFor(dayIndex);
            return boost > 0f ? $"{coins}\n+{boost:0}m 2x" : coins;
        }

        private void OnCollect()
        {
            collectButton.interactable = false;
            doubleButton.interactable = false;
            _daily.Claim();
        }

        private void OnDouble()
        {
            collectButton.interactable = false;
            doubleButton.interactable = false;
            _daily.ClaimWithAd();
        }

        private void OnClaimed(DailyRewardClaimed evt)
        {
            RefreshButton();
            root.SetActive(false);
        }

        private void OnOfflineClaimed(OfflineRewardClaimed evt) => TryAutoOpen();
    }
}
