using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    public sealed class DailyRewardServiceTests
    {
        private sealed class FixedClock : ITimeProvider
        {
            // Noon UTC stays on the same local calendar day in every time zone the tests could run in.
            public DateTime UtcNow { get; set; } = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
        }

        private DailyRewardConfig _config;
        private MonetizationConfig _monetization;
        private MetaModel _meta;
        private WalletModel _wallet;
        private FixedClock _clock;
        private MockAdsService _ads;
        private MockAnalyticsService _analytics;
        private IncomeBoost _boost;
        private DailyRewardService _daily;
        private double _income;
        private int _claimEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            EventBus.Subscribe<DailyRewardClaimed>(_ => _claimEvents++);
            _claimEvents = 0;
            _income = 0.0;
            _config = ScriptableObject.CreateInstance<DailyRewardConfig>();
            _monetization = ScriptableObject.CreateInstance<MonetizationConfig>();
            _meta = new MetaModel();
            _wallet = new WalletModel(0);
            _clock = new FixedClock();
            _ads = new MockAdsService(0.5f);
            _analytics = new MockAnalyticsService(false);
            _boost = new IncomeBoost(_monetization, _meta);
            _daily = new DailyRewardService(_config, _meta, _clock, () => _income, new EconomyService(_wallet),
                _boost, _ads, _analytics);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            UnityEngine.Object.DestroyImmediate(_config);
            UnityEngine.Object.DestroyImmediate(_monetization);
        }

        private void NextDay(int days = 1) => _clock.UtcNow = _clock.UtcNow.AddDays(days);

        [Test]
        public void FirstClaim_PaysDayOne_OncePerDay()
        {
            Assert.IsTrue(_daily.CanClaim);
            Assert.AreEqual(0, _daily.TodayIndex);

            _daily.Claim();
            _daily.Claim();

            Assert.AreEqual(_config.days[0].minimumCoins, _wallet.coins, "the second claim the same day pays nothing");
            Assert.AreEqual(1, _claimEvents);
            Assert.IsFalse(_daily.CanClaim);
            Assert.IsTrue(_daily.IsClaimed(0));
            Assert.AreEqual(1, _daily.Streak);
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.DailyRewardClaimed));
        }

        [Test]
        public void ConsecutiveDays_WalkTheCalendar_AndTheJackpotAddsBoostTime()
        {
            for (int day = 0; day < _config.days.Length; day++)
            {
                Assert.AreEqual(day, _daily.TodayIndex, $"claim {day + 1}");
                for (int earlier = 0; earlier < day; earlier++) Assert.IsTrue(_daily.IsClaimed(earlier));
                _daily.Claim();
                NextDay();
            }

            Assert.AreEqual(_config.days.Length, _meta.dailyStreak);
            float boostMinutes = 0f;
            foreach (DailyRewardDay day in _config.days) boostMinutes += day.boostMinutes;
            Assert.AreEqual(boostMinutes * 60f, _boost.RemainingSeconds, 0.01f, "boost days add free 2x time");
            Assert.AreEqual(0, _daily.TodayIndex, "after the last day the cycle starts over");
            Assert.IsFalse(_daily.IsClaimed(0));
        }

        [Test]
        public void MissingADay_ResetsTheStreak()
        {
            _daily.Claim();
            NextDay();
            _daily.Claim();
            NextDay(2);

            Assert.AreEqual(0, _daily.Streak);
            Assert.AreEqual(0, _daily.TodayIndex);
            Assert.IsFalse(_daily.ContinuesStreak);
            _daily.Claim();
            Assert.AreEqual(1, _meta.dailyStreak);
        }

        [Test]
        public void Coins_ScaleWithIncome()
        {
            _income = 10.0;
            long expected = (long)(10.0 * _config.days[0].incomeMinutes * 60.0);
            Assert.AreEqual(expected, _daily.TodayCoins);

            _daily.Claim();

            Assert.AreEqual(expected, _wallet.coins);
        }

        [Test]
        public void ClaimWithAd_PaysTheMultiplierAfterTheAd()
        {
            long single = _daily.TodayCoins;

            _daily.ClaimWithAd();
            Assert.AreEqual(0, _wallet.coins, "nothing is paid while the ad plays");
            _ads.Tick(1f);

            Assert.AreEqual(EconomyMath.ApplyMultiplier(single, _config.adMultiplier), _wallet.coins);
            Assert.AreEqual(AdPlacements.DailyDouble, _ads.LastPlacement);
            Assert.IsFalse(_daily.CanClaim);
        }

        [Test]
        public void MissingConfig_NeverOffersAReward()
        {
            var daily = new DailyRewardService(null, _meta, _clock, () => 1.0, new EconomyService(_wallet), _boost,
                _ads, _analytics);

            Assert.IsFalse(daily.CanClaim);
            daily.Claim();
            Assert.AreEqual(0, _wallet.coins);
        }

        [Test]
        public void ClaimIsSavedInTheMetaModel()
        {
            _daily.Claim();

            Assert.AreEqual(1, _meta.dailyStreak);
            Assert.AreEqual(DailyRewardCalendar.FormatDay(_clock.UtcNow.ToLocalTime()), _meta.dailyLastClaimDay);
        }
    }
}
