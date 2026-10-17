using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    public sealed class AchievementServiceTests
    {
        private AchievementTestData _data;
        private MonetizationConfig _monetization;
        private MetaModel _meta;
        private WalletModel _wallet;
        private IncomeBoost _boost;
        private MockAnalyticsService _analytics;
        private readonly Dictionary<AchievementGoal, long> _levels = new Dictionary<AchievementGoal, long>();
        private AchievementService _service;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _data = new AchievementTestData();
            _monetization = ScriptableObject.CreateInstance<MonetizationConfig>();
            _meta = new MetaModel { playerId = "me" };
            _wallet = new WalletModel(0);
            _boost = new IncomeBoost(_monetization, _meta);
            _analytics = new MockAnalyticsService(false);
            _levels.Clear();
            _levels[AchievementGoal.ReachTier] = 3;
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            EventBus.ClearAll();
            _data.Dispose();
            Object.DestroyImmediate(_monetization);
        }

        private AchievementBook Start(params AchievementDefinition[] definitions)
        {
            var book = new AchievementBook(definitions, new List<AchievementModel>());
            _service = new AchievementService(book, _meta, stand => stand == 0 ? "donuts" : "towels",
                goal => _levels.TryGetValue(goal, out long value) ? value : 0, new EconomyService(_wallet), _boost, _analytics);
            return book;
        }

        [Test]
        public void LevelGoals_CatchUpWithTheIslandOnStart()
        {
            AchievementBook book = Start(_data.Make("cabin", AchievementGoal.ReachTier, 2), _data.Make("hotel", AchievementGoal.ReachTier, 4));

            Assert.IsTrue(book.IsComplete(0), "an old save that already has the Bungalow gets the Cabin goal");
            Assert.AreEqual(3, book.Progress(1));
            _levels[AchievementGoal.ReachTier] = 4;
            EventBus.Publish(new TierUpgraded(0, 4));
            Assert.IsTrue(book.IsComplete(1));
        }

        [Test]
        public void GameEvents_AdvanceTheirGoals()
        {
            AchievementBook book = Start(
                _data.Make("donuts", AchievementGoal.SellProduct, 10, product: _data.Donuts),
                _data.Make("visitors", AchievementGoal.ServeVisitors, 10),
                _data.Make("quests", AchievementGoal.CompleteQuests, 10),
                _data.Make("visits", AchievementGoal.VisitIslands, 10),
                _data.Make("decor", AchievementGoal.OwnDecorations, 10),
                _data.Make("streak", AchievementGoal.DailyStreak, 7));

            EventBus.Publish(new SaleCompleted(0, 5, 1));
            EventBus.Publish(new SaleCompleted(1, 5, 2)); // towels
            EventBus.Publish(new VisitorPurchased(1, 1));
            EventBus.Publish(new QuestCompleted(0, 50));
            EventBus.Publish(new VisitStarted("kai", 10));
            EventBus.Publish(new VisitStarted("me", 0)); // own island does not count
            _levels[AchievementGoal.OwnDecorations] = 3;
            EventBus.Publish(new DecorationPlaced(2));
            _levels[AchievementGoal.DailyStreak] = 2;
            EventBus.Publish(new DailyRewardClaimed(1, 100, 0f));

            Assert.AreEqual(1, book.Progress(0), "donut sales only");
            Assert.AreEqual(1, book.Progress(1));
            Assert.AreEqual(1, book.Progress(2));
            Assert.AreEqual(1, book.Progress(3));
            Assert.AreEqual(3, book.Progress(4));
            Assert.AreEqual(2, book.Progress(5));
        }

        [Test]
        public void Claim_PaysCoinsAndBoost_Once_AndRewardsDoNotCountAsEarnings()
        {
            AchievementBook book = Start(
                _data.Make("first", AchievementGoal.EarnCoins, 100, coins: 500, boostMinutes: 10f),
                _data.Make("second", AchievementGoal.EarnCoins, 1000));
            int claims = 0;
            EventBus.Subscribe<AchievementClaimed>(_ => claims++);

            new EconomyService(_wallet).Earn(100);
            Assert.IsTrue(_service.Claim(0));
            Assert.IsFalse(_service.Claim(0));
            Assert.IsFalse(_service.Claim(1), "not complete");

            Assert.AreEqual(600, _wallet.coins);
            Assert.AreEqual(600f, _boost.RemainingSeconds, 0.01f, "10 minutes of 2x");
            Assert.AreEqual(1, claims);
            Assert.AreEqual(100, book.Progress(1), "the 500 coin reward is not counted as earned");
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.AchievementClaimed));
        }
    }
}
