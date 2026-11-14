#if BEACH_LEVELPLAY
using System;
using BeachTycoon.Core;
using BeachTycoon.Services;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace BeachTycoon.Platform
{
    /// <summary>
    /// Rewarded ads through LevelPlay (Unity's ad mediation). The SDK starts only once the player has answered the
    /// privacy question, with their answer as GDPR consent; a later change in Settings is passed on. One rewarded
    /// ad is kept loaded. Some networks report the reward just after the ad closes, so a closed ad waits a moment
    /// for it before telling the game.
    /// </summary>
    public sealed class LevelPlayAdsService : IAdsService, ITickable, IDisposable
    {
        private const float RetrySeconds = 30f;
        private const float RewardGraceSeconds = 1f;

        private readonly string _appKey;
        private readonly string _adUnitId;
        private LevelPlayRewardedAd _rewarded;
        private Action<bool> _pending;
        private bool _started;
        private bool _earned;
        private bool _closed;
        private float _closedFor;
        private float _retryIn = -1f;

        /// <param name="answered">Whether the privacy question was answered; until then nothing starts.</param>
        public LevelPlayAdsService(string appKey, string rewardedAdUnitId, bool answered, bool personalized)
        {
            _appKey = appKey;
            _adUnitId = rewardedAdUnitId;
            EventBus.Subscribe<PrivacyChanged>(OnPrivacyChanged);
            if (answered) Start(personalized);
        }

        public bool IsRewardedReady => _pending == null && _rewarded != null && _rewarded.IsAdReady();

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (!IsRewardedReady)
            {
                onFinished?.Invoke(false);
                return;
            }

            _pending = onFinished;
            _earned = false;
            _closed = false;
            _rewarded.ShowAd(); // placements are only for our analytics; the dashboard keeps one default
        }

        public void Tick(float deltaTime)
        {
            if (_closed)
            {
                _closedFor += Time.unscaledDeltaTime;
                if (_earned || _closedFor >= RewardGraceSeconds) Finish(_earned);
            }

            if (_retryIn < 0f) return;
            _retryIn -= Time.unscaledDeltaTime;
            if (_retryIn < 0f) _rewarded?.LoadAd();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<PrivacyChanged>(OnPrivacyChanged);
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            if (_rewarded == null) return;
            _rewarded.OnAdLoadFailed -= OnLoadFailed;
            _rewarded.OnAdDisplayFailed -= OnDisplayFailed;
            _rewarded.OnAdRewarded -= OnRewarded;
            _rewarded.OnAdClosed -= OnClosed;
            _rewarded.DestroyAd();
            _rewarded = null;
        }

        private void OnPrivacyChanged(PrivacyChanged evt)
        {
            if (_started) SetConsent(evt.PersonalizedAds);
            else Start(evt.PersonalizedAds);
        }

        private void Start(bool personalized)
        {
            _started = true;
            SetConsent(personalized);
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(_appKey);
        }

        private static void SetConsent(bool personalized)
        {
            LevelPlayPrivacySettings.SetGDPRConsent(personalized);
            LevelPlayPrivacySettings.SetCCPA(!personalized); // "only necessary" also opts out of data sale
        }

        private void OnInitSuccess(LevelPlayConfiguration configuration)
        {
            if (_rewarded != null) return;
            _rewarded = new LevelPlayRewardedAd(_adUnitId);
            _rewarded.OnAdLoadFailed += OnLoadFailed;
            _rewarded.OnAdDisplayFailed += OnDisplayFailed;
            _rewarded.OnAdRewarded += OnRewarded;
            _rewarded.OnAdClosed += OnClosed;
            _rewarded.LoadAd();
        }

        private void OnInitFailed(LevelPlayInitError error) => Debug.LogWarning($"Ads could not start: {error.ErrorMessage}");

        private void OnLoadFailed(LevelPlayAdError error) => _retryIn = RetrySeconds;

        private void OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Finish(false);
            _rewarded.LoadAd();
        }

        private void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward) => _earned = true;

        private void OnClosed(LevelPlayAdInfo info)
        {
            _closed = true;
            _closedFor = 0f;
            _rewarded.LoadAd(); // the next one loads while this result settles
        }

        private void Finish(bool rewarded)
        {
            _closed = false;
            Action<bool> callback = _pending;
            _pending = null;
            callback?.Invoke(rewarded);
        }
    }
}
#endif
