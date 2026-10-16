#if BEACH_ANALYTICS
using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;
using GameAnalytics = BeachTycoon.Services.IAnalyticsService;
using UgsAnalytics = Unity.Services.Analytics.AnalyticsService;

namespace BeachTycoon.Platform
{
    /// <summary>
    /// Game events sent to Unity Analytics, only after the player accepted on the privacy question. Events from
    /// before the services finish starting are held briefly and sent once data collection may begin. Turning
    /// consent off stops collection and asks Unity to delete what was collected.
    /// Custom events must also be defined in the Unity Cloud dashboard (Analytics > Event Manager).
    /// </summary>
    public sealed class UnityAnalyticsService : GameAnalytics, IDisposable
    {
        private const int MaxWaiting = 32;

        private readonly Queue<CustomEvent> _waiting = new Queue<CustomEvent>();
        private bool _ready;
        private bool _allowed;

        public UnityAnalyticsService(bool allowed)
        {
            _allowed = allowed;
            EventBus.Subscribe<PrivacyChanged>(OnPrivacyChanged);
            Initialize();
        }

        public void LogEvent(string eventName) => Record(new CustomEvent(eventName));

        public void LogEvent(string eventName, string parameter, long value) =>
            Record(new CustomEvent(eventName) { { parameter, value } });

        public void LogEvent(string eventName, string parameter, string value) =>
            Record(new CustomEvent(eventName) { { parameter, value } });

        public void Dispose() => EventBus.Unsubscribe<PrivacyChanged>(OnPrivacyChanged);

        private async void Initialize()
        {
            try
            {
                await UnityServices.InitializeAsync();
                _ready = true;
                if (_allowed) Start();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Analytics unavailable: {exception.Message}");
                _waiting.Clear();
            }
        }

        // Unity's consent state drives collection; Unspecified (not asked yet) collects nothing.
        private static void SetConsent(bool granted)
        {
            ConsentState state = EndUserConsent.GetConsentState();
            state.AnalyticsIntent = granted ? ConsentStatus.Granted : ConsentStatus.Denied;
            EndUserConsent.SetConsentState(state);
        }

        private void Start()
        {
            SetConsent(true);
            while (_waiting.Count > 0) UgsAnalytics.Instance.RecordEvent(_waiting.Dequeue());
        }

        private void Record(CustomEvent analyticsEvent)
        {
            if (!_allowed && _ready) return;
            if (_ready) UgsAnalytics.Instance.RecordEvent(analyticsEvent);
            else if (_waiting.Count < MaxWaiting) _waiting.Enqueue(analyticsEvent); // the answer may still come
        }

        private void OnPrivacyChanged(PrivacyChanged evt)
        {
            bool was = _allowed;
            _allowed = evt.AnalyticsAllowed;
            if (!_ready || was == _allowed) return;
            if (_allowed)
            {
                Start();
                return;
            }

            SetConsent(false);
            UgsAnalytics.Instance.RequestDataDeletion();
        }
    }
}
#endif
