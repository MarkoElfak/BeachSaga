using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// On boot, converts the time since the last save into pending coins (income rate x capped hours x
    /// efficiency; each stand manager raises the efficiency a little). The reward waits in the save until
    /// claimed, optionally doubled by a rewarded ad.
    /// </summary>
    public sealed class OfflineEarningsService
    {
        private const double SecondsPerHour = 3600.0;

        private readonly MetaModel _meta;
        private readonly IEconomyService _economy;
        private readonly IAdsService _ads;
        private readonly IAnalyticsService _analytics;
        private readonly MonetizationConfig _monetization;

        public OfflineEarningsService(GameDatabase database, ISaveService save, ITimeProvider clock,
            IncomeEstimator estimator, IEconomyService economy, IAdsService ads, IAnalyticsService analytics)
        {
            _meta = save.Data.meta;
            _economy = economy;
            _ads = ads;
            _analytics = analytics;
            _monetization = database.monetization;
            IncomePerSecond = estimator.IncomePerSecond();
            EconomyConfig config = database.economy;
            Efficiency = OfflineEarnings.Efficiency(config.offlineEfficiency, config.offlineEfficiencyPerManager,
                CountManagers(save.Data.stands), config.offlineEfficiencyMax);

            if (save.LoadedFromDisk && save.PreviousSessionUtc.HasValue)
            {
                Accrue(clock.UtcNow - save.PreviousSessionUtc.Value, database.economy);
            }
        }

        /// <summary>Income rate used for the estimate (coins per second at full efficiency).</summary>
        public double IncomePerSecond { get; }

        /// <summary>Share of the income rate paid while away.</summary>
        public float Efficiency { get; }

        public bool HasPending => _meta.pendingOfflineCoins > 0;
        public long PendingCoins => _meta.pendingOfflineCoins;
        public float PendingSeconds => _meta.pendingOfflineSeconds;
        public long DoubledCoins => EconomyMath.ApplyMultiplier(_meta.pendingOfflineCoins, _monetization.offlineRewardMultiplier);

        public void Claim()
        {
            Grant(1f);
        }

        public void ClaimWithAd()
        {
            if (!_ads.IsRewardedReady)
            {
                Claim();
                return;
            }

            _ads.ShowRewarded(AdPlacements.OfflineDouble, watched => Grant(watched ? _monetization.offlineRewardMultiplier : 1f));
        }

        private void Accrue(TimeSpan absence, EconomyConfig economy)
        {
            double seconds = OfflineEarnings.CappedSeconds(absence, economy.offlineCapHours, economy.offlineMinSeconds);
            long coins = OfflineEarnings.Calculate(IncomePerSecond, absence, Efficiency,
                economy.offlineCapHours, economy.offlineMinSeconds);
            if (coins <= 0)
            {
                return;
            }

            // Unclaimed rewards from earlier sessions stack, but never beyond one full cap.
            long cap = OfflineEarnings.Calculate(IncomePerSecond, TimeSpan.FromHours(economy.offlineCapHours),
                Efficiency, economy.offlineCapHours, 0f);
            _meta.pendingOfflineCoins = Math.Min(cap, _meta.pendingOfflineCoins + coins);
            _meta.pendingOfflineSeconds = (float)Math.Min(economy.offlineCapHours * SecondsPerHour, _meta.pendingOfflineSeconds + seconds);
        }

        private void Grant(float multiplier)
        {
            if (!HasPending)
            {
                return;
            }

            long amount = EconomyMath.ApplyMultiplier(_meta.pendingOfflineCoins, multiplier);
            _meta.pendingOfflineCoins = 0;
            _meta.pendingOfflineSeconds = 0f;
            _economy.Earn(amount);
            _analytics.LogEvent(AnalyticsEvents.OfflineRewardClaimed, "coins", amount);
            EventBus.Publish(new OfflineRewardClaimed(amount));
        }

        private static int CountManagers(System.Collections.Generic.List<StandModel> stands)
        {
            int count = 0;
            for (int i = 0; i < stands.Count; i++) if (stands[i].manager) count++;
            return count;
        }
    }
}
