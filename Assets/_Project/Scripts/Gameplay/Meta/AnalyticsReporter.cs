using System;
using BeachTycoon.Core;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>Forwards gameplay events to IAnalyticsService (offline_reward_claimed is logged by OfflineEarningsService).</summary>
    public sealed class AnalyticsReporter : IDisposable
    {
        private readonly IAnalyticsService _analytics;

        public AnalyticsReporter(IAnalyticsService analytics)
        {
            _analytics = analytics;
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<ProductUnlocked>(OnProductUnlocked);
        }

        public void LogSessionStart(bool returningPlayer)
        {
            _analytics.LogEvent(AnalyticsEvents.SessionStart, "returning", returningPlayer ? 1 : 0);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<ProductUnlocked>(OnProductUnlocked);
        }

        private void OnTierUpgraded(TierUpgraded evt)
        {
            if (evt.LotIndex == BuildingManager.MainLotIndex)
            {
                _analytics.LogEvent(AnalyticsEvents.TierUpgraded, "tier", evt.Tier);
            }
        }

        private void OnProductUnlocked(ProductUnlocked evt)
        {
            _analytics.LogEvent(AnalyticsEvents.ProductUnlocked, "product", evt.ProductId);
        }
    }
}
