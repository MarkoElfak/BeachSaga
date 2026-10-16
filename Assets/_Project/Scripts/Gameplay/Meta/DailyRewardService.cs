using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Daily login rewards on the player's local calendar: one claim per day, a streak that grows on
    /// consecutive days and a reward cycle that starts over after the last day. Coins scale with the
    /// island's income; some days add free 2x boost time. Claiming can be doubled by a rewarded ad.
    /// </summary>
    public sealed class DailyRewardService
    {
        private readonly DailyRewardConfig _config;
        private readonly MetaModel _meta;
        private readonly ITimeProvider _clock;
        private readonly Func<double> _incomePerSecond;
        private readonly IEconomyService _economy;
        private readonly IncomeBoost _boost;
        private readonly IAdsService _ads;
        private readonly IAnalyticsService _analytics;

        public DailyRewardService(DailyRewardConfig config, MetaModel meta, ITimeProvider clock,
            Func<double> incomePerSecond, IEconomyService economy, IncomeBoost boost, IAdsService ads,
            IAnalyticsService analytics)
        {
            _config = config;
            _meta = meta;
            _clock = clock;
            _incomePerSecond = incomePerSecond;
            _economy = economy;
            _boost = boost;
            _ads = ads;
            _analytics = analytics;
        }

        public int CycleLength => _config != null && _config.days != null ? _config.days.Length : 0;
        public bool OpenOnLaunch => _config != null && _config.openOnLaunch;
        public bool CanClaim => CycleLength > 0 && DailyRewardCalendar.CanClaim(LastClaimDay, Today);

        /// <summary>True when today's claim continues yesterday's streak (false on day 1 or after a break).</summary>
        public bool ContinuesStreak => DailyRewardCalendar.ContinuesStreak(LastClaimDay, Today);

        /// <summary>Consecutive days claimed, 0 once a day has been missed.</summary>
        public int Streak => DailyRewardCalendar.CurrentStreak(LastClaimDay, _meta.dailyStreak, Today);

        /// <summary>0-based calendar position of today's reward (claimable now, or already claimed today).</summary>
        public int TodayIndex => DailyRewardCalendar.DayIndex(LastClaimDay, _meta.dailyStreak, Today, CycleLength);

        public long TodayCoins => CoinsFor(TodayIndex);
        public long DoubledCoins => EconomyMath.ApplyMultiplier(TodayCoins, AdMultiplier);
        private float AdMultiplier => _config != null ? _config.adMultiplier : 1f;

        // The calendar runs on the player's local midnight, like most mobile games.
        private DateTime Today => _clock.UtcNow.ToLocalTime().Date;
        private DateTime? LastClaimDay => DailyRewardCalendar.ParseDay(_meta.dailyLastClaimDay);

        /// <summary>Days before today in the current cycle are done; today is done once claimed.</summary>
        public bool IsClaimed(int dayIndex)
        {
            int today = TodayIndex;
            return dayIndex < today || (dayIndex == today && !CanClaim);
        }

        /// <summary>Coins a day pays at the island's current income.</summary>
        public long CoinsFor(int dayIndex)
        {
            if (dayIndex < 0 || dayIndex >= CycleLength)
            {
                return 0;
            }

            DailyRewardDay day = _config.days[dayIndex];
            return DailyRewardCalendar.Coins(_incomePerSecond(), day.incomeMinutes, day.minimumCoins);
        }

        public float BoostMinutesFor(int dayIndex)
        {
            return dayIndex >= 0 && dayIndex < CycleLength ? _config.days[dayIndex].boostMinutes : 0f;
        }

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

            _ads.ShowRewarded(AdPlacements.DailyDouble, watched => Grant(watched ? AdMultiplier : 1f));
        }

        private void Grant(float multiplier)
        {
            if (!CanClaim)
            {
                return;
            }

            DateTime today = Today;
            DateTime? last = LastClaimDay;
            int dayIndex = TodayIndex;
            long coins = EconomyMath.ApplyMultiplier(CoinsFor(dayIndex), multiplier);
            float boostMinutes = BoostMinutesFor(dayIndex);

            _meta.dailyStreak = DailyRewardCalendar.StreakAfterClaim(last, _meta.dailyStreak, today);
            _meta.dailyLastClaimDay = DailyRewardCalendar.FormatDay(today);
            _economy.Earn(coins);
            _boost.Extend(boostMinutes * 60f);
            _analytics.LogEvent(AnalyticsEvents.DailyRewardClaimed, "day", dayIndex + 1);
            EventBus.Publish(new DailyRewardClaimed(dayIndex, coins, boostMinutes));
        }
    }
}
