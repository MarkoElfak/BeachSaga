using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>New season: when it may start, what the reset clears and keeps, and the seashell income bonus.</summary>
    public sealed class SeasonServiceTests
    {
        private EconomyConfig _config;
        private GameSaveData _data;
        private MockAnalyticsService _analytics;
        private SeasonService _season;
        private int _reboots;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _data = GameSaveData.CreateNew(100);
            _data.lots.Add(new LotModel(0) { state = LotState.Built, tier = 5, placed = true, x = 3f, z = -2f, yaw = 90f });
            _data.lots.Add(new LotModel(1) { state = LotState.Built, tier = 1, variant = 2 });
            _data.stands.Add(new StandModel { productId = "donuts", level = 4, manager = true });
            _data.neighbours.Add(new NeighbourModel { name = "Ana", lotIndex = 1 });
            _data.decorations.Add(new DecorationModel { decorationId = "palm" });
            _data.meta.dailyStreak = 3;
            _data.wallet.coins = 900000;
            _data.wallet.seasonEarned = 2000000;
            _analytics = new MockAnalyticsService(false);
            _reboots = 0;
            _season = new SeasonService(_config, _data, _analytics, () => _reboots++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            EventBus.ClearAll();
        }

        [Test]
        public void NeedsTheUnlockTier()
        {
            _data.lots[0].tier = 4;
            Assert.IsFalse(_season.CanStartNewSeason);
            Assert.IsFalse(_season.StartNewSeason(100));
            Assert.AreEqual(0, _reboots);
            _data.lots[0].tier = 5;
            Assert.IsTrue(_season.CanStartNewSeason);
        }

        [Test]
        public void NeedsAtLeastOneSeashell()
        {
            _data.wallet.seasonEarned = 1000;
            Assert.IsTrue(_season.IsTierReached);
            Assert.IsFalse(_season.CanStartNewSeason);
        }

        [Test]
        public void StartingPaysSeashellsAndReboots()
        {
            Assert.AreEqual(8L, _season.SeashellsToEarn);
            Assert.AreEqual(1.8f, _season.NextIncomeMultiplier, 1e-5f);
            Assert.IsTrue(_season.StartNewSeason(100));
            Assert.AreEqual(8L, _season.Seashells);
            Assert.AreEqual(2, _season.Season);
            Assert.AreEqual(1.8f, _season.IncomeMultiplier, 1e-5f);
            Assert.AreEqual(1, _reboots);
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.SeasonStarted));
        }

        [Test]
        public void ResetClearsCoinsBuildingAndStands()
        {
            _season.StartNewSeason(100);
            Assert.AreEqual(100L, _data.wallet.coins);
            Assert.AreEqual(0L, _data.wallet.seasonEarned);
            Assert.AreEqual(0, _data.stands.Count);
            LotModel main = _data.lots[0];
            Assert.AreEqual(0, main.tier);
            Assert.AreEqual(LotState.Empty, main.state);
            Assert.IsTrue(main.placed);
            Assert.AreEqual(3f, main.x);
            Assert.AreEqual(-2f, main.z);
            Assert.AreEqual(90f, main.yaw);
        }

        [Test]
        public void ResetKeepsNeighboursDecorationsAndMeta()
        {
            _season.StartNewSeason(100);
            Assert.AreEqual(1, _data.neighbours.Count);
            Assert.AreEqual(1, _data.decorations.Count);
            Assert.AreEqual(LotState.Built, _data.lots[1].state);
            Assert.AreEqual(2, _data.lots[1].variant);
            Assert.AreEqual(3, _data.meta.dailyStreak);
        }

        [Test]
        public void SeashellsAddUpOverSeasons()
        {
            _season.StartNewSeason(100);
            _data.lots[0].tier = 5;
            _data.wallet.seasonEarned = 100000;
            Assert.IsTrue(_season.StartNewSeason(100));
            Assert.AreEqual(10L, _season.Seashells);
            Assert.AreEqual(3, _season.Season);
        }

        [Test]
        public void SalesIncludeTheSeasonBonus()
        {
            _season.StartNewSeason(100);
            var monetization = ScriptableObject.CreateInstance<MonetizationConfig>();
            var sales = new SaleMultipliers(new IncomeBoost(monetization, new MetaModel()), _season);
            Assert.AreEqual(180L, sales.Apply(100, null));
            Object.DestroyImmediate(monetization);
        }
    }
}
