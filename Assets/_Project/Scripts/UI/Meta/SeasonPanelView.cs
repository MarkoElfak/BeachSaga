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
    /// HUD "New season" button (shown once the Resort is reached) and the confirmation panel: what the new
    /// season pays in seashells, the income bonus before and after, and exactly what starts over or stays.
    /// </summary>
    public sealed class SeasonPanelView : MonoBehaviour
    {
        private const string Resets = "<b>Starts over:</b> coins, the main building (back to an empty lot), every stand with its level, and your staff.";
        private const string Keeps = "<b>Stays:</b> seashells, neighbours and their houses, decorations, mini-game levels, trophies, the daily streak and settings.";
        private static readonly Color Locked = new Color(0.72f, 0.72f, 0.72f);

        [SerializeField] private Button openButton = null;
        [SerializeField] private TMP_Text openLabel = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private TMP_Text summary = null;
        [SerializeField] private TMP_Text details = null;
        [SerializeField] private Button startButton = null;
        [SerializeField] private TMP_Text startLabel = null;

        private SeasonService _season;
        private EconomyConfig _economy;
        private string _unlockTierName;
        private Color _readyColor;

        public bool IsOpen => root.activeSelf;
        public bool IsButtonShown => openButton.gameObject.activeSelf;
        public string ButtonText => openLabel.text;
        public string SummaryText => summary.text;
        public bool CanStart => startButton.interactable;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _season))
            {
                openButton.gameObject.SetActive(false);
                return;
            }

            var database = ServiceLocator.Get<GameDatabase>();
            _economy = database.economy;
            TierDefinition unlockTier = database.GetTier(_season.UnlockTier);
            _unlockTierName = unlockTier != null ? unlockTier.displayName : $"tier {_season.UnlockTier}";
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            startButton.onClick.AddListener(StartSeason);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            details.text = Resets + "\n\n" + Keeps;
            _readyColor = startButton.image.color;
            RefreshButton();
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            startButton.onClick.RemoveListener(StartSeason);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
        }

        public void Open()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            root.SetActive(true);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        private void StartSeason()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _season.StartNewSeason(_economy.startingCoins); // reboots the game from the reset save
        }

        private void RefreshButton()
        {
            bool show = _season.IsTierReached || _season.Seashells > 0;
            if (openButton.gameObject.activeSelf != show) openButton.gameObject.SetActive(show);
            openLabel.text = _season.IsTierReached ? "New season" : $"Season {_season.Season}";
        }

        private void Refresh()
        {
            long shells = _season.SeashellsToEarn;
            summary.text = $"Season {_season.Season}: earned {NumberFormatter.Format(_season.SeasonEarned)} coins\n"
                + $"New season pays <b>{NumberFormatter.Format(shells)} seashells</b>\n"
                + $"All income +{Percent(_season.IncomeMultiplier)}% -> <b>+{Percent(_season.NextIncomeMultiplier)}%</b>";

            startButton.interactable = _season.CanStartNewSeason;
            startButton.image.color = _season.CanStartNewSeason ? _readyColor : Locked;
            startLabel.text = _season.CanStartNewSeason ? "Start new season"
                : !_season.IsTierReached ? $"Reach the {_unlockTierName} first" : "Earn more coins first";
        }

        private static string Percent(float multiplier) => Mathf.RoundToInt((multiplier - 1f) * 100f).ToString();

        private void OnTierUpgraded(TierUpgraded evt) => RefreshButton();

        private void OnCoinsChanged(CoinsChanged evt)
        {
            if (IsOpen) Refresh();
        }
    }
}
