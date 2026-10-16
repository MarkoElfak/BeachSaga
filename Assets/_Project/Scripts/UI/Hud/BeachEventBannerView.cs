using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>HUD banner for the running beach event: title, what it does and a countdown. Hidden otherwise.</summary>
    public sealed class BeachEventBannerView : MonoBehaviour
    {
        // TMP's "{n:0}" means zero decimals; seconds are split into tens and ones to keep two digits.
        private const string TimerFormat = "{0:0}:{1:0}{2:0}";
        private const int SecondsPerMinute = 60;

        [SerializeField] private GameObject banner = null;
        [SerializeField] private Image background = null;
        [SerializeField] private TMP_Text title = null;
        [SerializeField] private TMP_Text detail = null;
        [SerializeField] private TMP_Text timer = null;

        private BeachEventService _events;
        private int _shownSeconds = -1;

        public bool IsShowing => banner.activeSelf;
        public string TitleText => title.text;
        public string TimerText => timer.text;

        private void Start()
        {
            ServiceLocator.TryGet(out _events);
            EventBus.Subscribe<BeachEventStarted>(OnStarted);
            EventBus.Subscribe<BeachEventEnded>(OnEnded);
            Show();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BeachEventStarted>(OnStarted);
            EventBus.Unsubscribe<BeachEventEnded>(OnEnded);
        }

        private void OnStarted(BeachEventStarted evt) => Show();
        private void OnEnded(BeachEventEnded evt) => Show();

        private void Show()
        {
            BeachEventDefinition active = _events != null ? _events.Active : null;
            banner.SetActive(active != null);
            if (active == null) return;

            background.color = active.bannerColor;
            title.SetText(active.title);
            detail.SetText(active.description);
            _shownSeconds = -1;
            UpdateTimer();
        }

        private void Update()
        {
            if (_events != null && _events.IsActive) UpdateTimer();
        }

        private void UpdateTimer()
        {
            int seconds = Mathf.CeilToInt(_events.RemainingSeconds);
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            int secondsPart = seconds % SecondsPerMinute;
            timer.SetText(TimerFormat, seconds / SecondsPerMinute, secondsPart / 10, secondsPart % 10);
        }
    }
}
