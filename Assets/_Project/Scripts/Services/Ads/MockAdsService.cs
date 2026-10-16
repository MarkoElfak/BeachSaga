using System;
using BeachTycoon.Core;

namespace BeachTycoon.Services
{
    /// <summary>No-op rewarded ads: "plays" for a moment, then always rewards. Replace with a real SDK later.</summary>
    public sealed class MockAdsService : IAdsService, ITickable
    {
        private readonly float _playSeconds;
        private Action<bool> _pending;
        private float _remaining;

        public MockAdsService(float playSeconds)
        {
            _playSeconds = playSeconds;
        }

        public bool IsRewardedReady => _pending == null;

        /// <summary>Placement of the last ad shown, for tests and analytics.</summary>
        public string LastPlacement { get; private set; }

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (_pending != null)
            {
                onFinished?.Invoke(false);
                return;
            }

            LastPlacement = placement;
            _pending = onFinished;
            _remaining = _playSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (_pending == null)
            {
                return;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            Action<bool> callback = _pending;
            _pending = null;
            callback?.Invoke(true);
        }
    }
}
