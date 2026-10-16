using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Beach events: scheduling, which ones can run, and their effect on prices, demand and spawns.</summary>
    public sealed class BeachEventServiceTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private BeachEventConfig _config;
        private BeachEventDefinition _bus, _heat, _surf;
        private ProductDefinition _donuts, _drinks;
        private StandService _stands;
        private BeachEventService _events;
        private int _started, _ended;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            var database = Asset<GameDatabase>();
            database.economy = Asset<EconomyConfig>();
            database.world = Asset<WorldLayoutConfig>();
            database.tiers = new TierDefinition[0];
            _donuts = Product("donuts", 10);
            _drinks = Product("drinks", 20);
            ProductDefinition surf = Product("surf_lessons", 60);
            database.products = new[] { _donuts, _drinks, surf };

            _bus = Event("bus", 1, null, 1f, 0f, 2f, 3);
            _heat = Event("heat", 3, _drinks, 1.5f, 0.6f, 1f, 0);
            _surf = Event("surf", 4, surf, 2f, 0f, 1f, 0);
            _config = Asset<BeachEventConfig>();
            _config.firstEventSeconds = 100f;
            _config.gapSecondsRange = new Vector2(50f, 50f);
            _config.events = new[] { _bus, _heat, _surf };

            var economy = new EconomyService(new WalletModel(0));
            var lots = new List<LotModel> { new LotModel(0) { state = LotState.Built, tier = 3 } };
            var buildings = new BuildingManager(database, economy, lots);
            _events = new BeachEventService(_config, buildings, new MockAnalyticsService(false));
            var models = new List<StandModel> { new StandModel { productId = "donuts" }, new StandModel { productId = "drinks" } };
            _stands = new StandService(database, economy, buildings, models,
                new SaleMultipliers(new IncomeBoost(Asset<MonetizationConfig>(), new MetaModel()), null, _events));
            _events.AttachStands(_stands);

            _started = _ended = 0;
            EventBus.Subscribe<BeachEventStarted>(_ => _started++);
            EventBus.Subscribe<BeachEventEnded>(_ => _ended++);
        }

        [TearDown]
        public void TearDown()
        {
            _stands.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void FirstEvent_WaitsForTheDelay_ThenARunnableOneStarts()
        {
            _events.Tick(99f);
            Assert.IsFalse(_events.IsActive);
            _events.Tick(2f);
            Assert.IsTrue(_events.IsActive);
            Assert.AreEqual(1, _started);
            Assert.AreNotSame(_surf, _events.Active, "no surf stand and tier 3: the surf contest cannot run");
        }

        [Test]
        public void Events_NeedTheirTierAndStand()
        {
            Assert.IsTrue(_events.CanRun(_bus));
            Assert.IsTrue(_events.CanRun(_heat));
            Assert.IsFalse(_events.CanRun(_surf));
            Assert.IsFalse(_events.Begin(_surf));
            Assert.AreEqual(0, _started);
        }

        [Test]
        public void Event_EndsAfterItsDuration_ThenWaitsAGap()
        {
            Assert.IsTrue(_events.Begin(_bus));
            _events.Tick(_bus.durationSeconds - 1f);
            Assert.IsTrue(_events.IsActive);
            Assert.AreEqual(1f, _events.RemainingSeconds, 1e-3f);
            _events.Tick(2f);
            Assert.IsFalse(_events.IsActive);
            Assert.AreEqual(1, _ended);
            Assert.AreEqual(50f, _events.SecondsUntilNext, 1e-3f);
        }

        [Test]
        public void PriceMultiplier_AppliesOnlyToTheEventProduct_WhileItRuns()
        {
            _events.Begin(_heat);
            Assert.AreEqual(1.5f, _events.PriceMultiplier(_drinks));
            Assert.AreEqual(1f, _events.PriceMultiplier(_donuts));
            _events.End();
            Assert.AreEqual(1f, _events.PriceMultiplier(_drinks));
        }

        [Test]
        public void HeatwaveSale_IsWorthMore()
        {
            long amount = 0;
            EventBus.Subscribe<SaleCompleted>(evt => amount = evt.Amount);
            StandController drinks = _stands.FindByProduct("drinks");
            _events.Begin(_heat);
            drinks.TryEnqueueOrder(7, 1f);
            drinks.Tick(_drinks.serveSeconds + 0.01f);
            Assert.AreEqual(30, amount, "20 coins x 1.5");
            Assert.AreEqual(1, drinks.TotalOrders);
        }

        [Test]
        public void SpawnRateAndDemand_FollowTheRunningEvent()
        {
            Assert.AreEqual(1f, _events.SpawnRateMultiplier);
            Assert.IsNull(_events.DemandProduct);
            _events.Begin(_bus);
            Assert.AreEqual(2f, _events.SpawnRateMultiplier);
            Assert.IsNull(_events.DemandProduct);
            _events.Begin(_heat);
            Assert.AreEqual(1, _ended, "starting another event ends the running one");
            Assert.AreSame(_drinks, _events.DemandProduct);
            Assert.AreEqual(0.6f, _events.DemandChance);
        }

        [Test]
        public void StandPicker_SendsVisitorsToTheEventProduct()
        {
            var visitor = Asset<VisitorTypeDefinition>();
            visitor.preferredProducts = new[] { _donuts };
            var picker = new StandPicker(_stands, new System.Random(1), _events);
            Assert.AreEqual("donuts", picker.Pick(visitor).Product.id, "no event: the preferred product");

            _heat.demandChance = 1f;
            _events.Begin(_heat);
            for (int i = 0; i < 4; i++) Assert.AreEqual("drinks", picker.Pick(visitor).Product.id);
        }

        private ProductDefinition Product(string id, long price)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.price = price;
            product.serveSeconds = 2f;
            return product;
        }

        private BeachEventDefinition Event(string id, int minTier, ProductDefinition product, float price, float demand,
            float spawnRate, int burst)
        {
            var definition = Asset<BeachEventDefinition>();
            definition.id = id;
            definition.minTier = minTier;
            definition.product = product;
            definition.priceMultiplier = price;
            definition.demandChance = demand;
            definition.spawnRateMultiplier = spawnRate;
            definition.burstVisitors = burst;
            definition.durationSeconds = 60f;
            return definition;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
