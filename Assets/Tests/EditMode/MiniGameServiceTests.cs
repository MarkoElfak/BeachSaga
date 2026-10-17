using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Mini-game progress: which runs count, the saved level count, events and the sales bonus.</summary>
    public sealed class MiniGameServiceTests
    {
        private GameDatabase _database;
        private ProductDefinition _donuts, _towels;
        private MiniGameDefinition _game;
        private List<MiniGameProgress> _progress;
        private MockAnalyticsService _analytics;
        private MiniGameService _service;
        private int _passedEvents, _lastPassed;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _donuts = Product("donuts");
            _towels = Product("towels");
            _game = ScriptableObject.CreateInstance<MiniGameDefinition>();
            _game.id = "donut_rush";
            _game.product = _donuts;
            _game.levels = new MiniGameLevel[3];
            _database = ScriptableObject.CreateInstance<GameDatabase>();
            _database.miniGames = new[] { _game };
            _progress = new List<MiniGameProgress>();
            _analytics = new MockAnalyticsService(false);
            _service = new MiniGameService(_database, _progress, _analytics);
            _passedEvents = 0;
            EventBus.Subscribe<MiniGameLevelPassed>(OnPassed);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            Object.DestroyImmediate(_game);
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_donuts);
            Object.DestroyImmediate(_towels);
        }

        [Test]
        public void FindsTheGameForAProduct()
        {
            Assert.AreSame(_game, _service.ForProduct("donuts"));
            Assert.IsNull(_service.ForProduct("towels"));
            Assert.AreEqual(0, _service.LevelsPassed("towels"));
        }

        [Test]
        public void PassingTheNextLevelCountsAndAnnouncesIt()
        {
            Assert.IsTrue(_service.ReportResult(_game, 0, true));
            Assert.AreEqual(1, _service.LevelsPassed(_game));
            Assert.AreEqual(1, _progress.Count, "saved in the progress list");
            Assert.AreEqual(1, _passedEvents);
            Assert.AreEqual(1, _lastPassed);
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.MiniGameLevelPassed));
            Assert.AreEqual(1, _service.NextLevelIndex(_game));
        }

        [Test]
        public void LostRunsAndReplaysGiveNothing()
        {
            Assert.IsFalse(_service.ReportResult(_game, 0, false), "lost");
            Assert.IsTrue(_service.ReportResult(_game, 0, true));
            Assert.IsFalse(_service.ReportResult(_game, 0, true), "replay of a passed level");
            Assert.IsFalse(_service.ReportResult(_game, 2, true), "skipping ahead");
            Assert.AreEqual(1, _service.LevelsPassed(_game));
            Assert.AreEqual(1, _passedEvents);
        }

        [Test]
        public void StopsAtTheLastLevel()
        {
            for (int i = 0; i < 3; i++) Assert.IsTrue(_service.ReportResult(_game, i, true), $"level {i + 1}");
            Assert.IsTrue(_service.IsComplete(_game));
            Assert.AreEqual(2, _service.NextLevelIndex(_game), "the last level stays playable");
            Assert.IsFalse(_service.ReportResult(_game, 2, true));
            Assert.AreEqual(3, _service.LevelsPassed(_game));
        }

        [Test]
        public void EachLevelAddsTheBonusToThatProductOnly()
        {
            Assert.AreEqual(1f, _service.IncomeMultiplier(_donuts));
            _service.ReportResult(_game, 0, true);
            _service.ReportResult(_game, 1, true);
            Assert.AreEqual(1.2f, _service.IncomeMultiplier(_donuts), 1e-5f);
            Assert.AreEqual(1f, _service.IncomeMultiplier(_towels));
            Assert.AreEqual(1f, _service.IncomeMultiplier(null));
        }

        [Test]
        public void SalesIncludeTheMiniGameBonus()
        {
            _service.ReportResult(_game, 0, true);
            var monetization = ScriptableObject.CreateInstance<MonetizationConfig>();
            var sales = new SaleMultipliers(new IncomeBoost(monetization, new MetaModel()), null, null, _service);
            Assert.AreEqual(110L, sales.Apply(100, _donuts));
            Assert.AreEqual(100L, sales.Apply(100, _towels));
            Object.DestroyImmediate(monetization);
        }

        [Test]
        public void SavedProgressIsReadBack()
        {
            _progress.Add(new MiniGameProgress { gameId = "donut_rush", levelsPassed = 2 });
            Assert.AreEqual(2, _service.LevelsPassed("donuts"));
            Assert.IsTrue(_service.ReportResult(_game, 2, true));
        }

        private void OnPassed(MiniGameLevelPassed evt)
        {
            _passedEvents++;
            _lastPassed = evt.LevelsPassed;
        }

        private static ProductDefinition Product(string id)
        {
            var product = ScriptableObject.CreateInstance<ProductDefinition>();
            product.id = id;
            return product;
        }
    }
}
