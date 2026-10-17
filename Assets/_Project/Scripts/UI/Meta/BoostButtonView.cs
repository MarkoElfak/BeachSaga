using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>HUD button: watch a rewarded ad for 2x income. Shows the remaining time while active.</summary>
    public sealed class BoostButtonView : MonoBehaviour
    {
        // TMP's "{n:0}" means zero decimals; seconds are split into tens and ones to keep two digits.
        private const string ActiveFormat = "2x  {0:0}:{1:0}{2:0}";
        private const string IdleText = "2x income (ad)";
        private const string FreeText = "2x income";
        private const float SecondsPerMinute = 60f;

        [SerializeField] private Button button = null;
        [SerializeField] private TMP_Text label = null;

        private IncomeBoost _boost;
        private IAdsService _ads;
        private int _shownSeconds = -1;
        private bool _shownFree;

        public string LabelText => label.text;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _boost))
            {
                return;
            }

            _ads = ServiceLocator.Get<IAdsService>();
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            if (_boost == null)
            {
                return;
            }

            int seconds = Mathf.CeilToInt(_boost.RemainingSeconds);
            bool free = AdWording.AdsRemoved(_ads);
            if (seconds == _shownSeconds && free == _shownFree)
            {
                return;
            }

            _shownSeconds = seconds;
            _shownFree = free;
            button.interactable = !_boost.IsActive && _ads.IsRewardedReady;
            if (_boost.IsActive)
            {
                int secondsPart = seconds % (int)SecondsPerMinute;
                label.SetText(ActiveFormat, Mathf.Floor(seconds / SecondsPerMinute), secondsPart / 10, secondsPart % 10);
            }
            else
            {
                label.SetText(free ? FreeText : IdleText);
            }
        }

        private void OnClicked()
        {
            if (_boost.IsActive || !_ads.IsRewardedReady)
            {
                return;
            }

            button.interactable = false;
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _ads.ShowRewarded(AdPlacements.IncomeBoost, OnAdFinished);
        }

        private void OnAdFinished(bool watched)
        {
            if (watched)
            {
                _boost.Activate();
            }

            _shownSeconds = -1; // refresh label and interactable state
        }
    }
}
