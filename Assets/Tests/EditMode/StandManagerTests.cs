using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Hiring stand managers (StandService) and their offline bonus.</summary>
    public sealed class StandManagerTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private GameDatabase _database;
        private EconomyService _economy;
        private StandService _stands;
        private List<StandModel> _models;
        private ProductDefinition _donuts;
        private int _hired;
        private string _hiredProduct;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _database = Asset<GameDatabase>();
            _database.economy = Asset<EconomyConfig>();
            _database.world = Asset<WorldLayoutConfig>();
            _donuts = Asset<ProductDefinition>();
            _donuts.id = "donuts";
            _donuts.managerCost = 500;
            _database.products = new[] { _donuts };
            _database.tiers = new TierDefinition[0];

            _economy = new EconomyService(new WalletModel(0));
            var buildings = new BuildingManager(_database, _economy, new List<LotModel>());
            var boost = new IncomeBoost(Asset<MonetizationConfig>(), new MetaModel());
            _models = new List<StandModel> { new StandModel { productId = "donuts" } };
            _stands = new StandService(_database, _economy, buildings, _models, new SaleMultipliers(boost));
            _hired = 0;
            EventBus.Subscribe<ManagerHired>(evt =>
            {
                _hired++;
                _hiredProduct = evt.ProductId;
            });
        }

        [TearDown]
        public void TearDown()
        {
            _stands.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        [Test]
        public void Hire_NeedsTheManagerCost()
        {
            StandController stand = _stands.Get(0);
            _economy.Earn(499);
            Assert.IsFalse(_stands.CanHireManager(stand));
            Assert.IsFalse(_stands.TryHireManager(stand));
            Assert.AreEqual(499, _economy.Coins);
            Assert.IsFalse(_models[0].manager);
            Assert.AreEqual(0, _hired);
        }

        [Test]
        public void Hire_PaysOnceSavesAndPublishes()
        {
            StandController stand = _stands.Get(0);
            _economy.Earn(1200);
            Assert.IsTrue(_stands.TryHireManager(stand));
            Assert.AreEqual(700, _economy.Coins);
            Assert.IsTrue(_models[0].manager, "saved in the stand model");
            Assert.AreEqual(1, _stands.ManagerCount);
            Assert.AreEqual(1, _hired);
            Assert.AreEqual("donuts", _hiredProduct);

            Assert.IsFalse(_stands.CanHireManager(stand), "one manager per stand");
            Assert.IsFalse(_stands.TryHireManager(stand));
            Assert.AreEqual(700, _economy.Coins);
        }

        [Test]
        public void SavedManagers_AreCounted()
        {
            _models[0].manager = true;
            Assert.AreEqual(1, _stands.ManagerCount);
        }

        [Test]
        public void OfflineEfficiency_RisesPerManagerUpToTheCap()
        {
            Assert.AreEqual(0.5f, OfflineEarnings.Efficiency(0.5f, 0.05f, 0, 0.75f), 1e-6f);
            Assert.AreEqual(0.65f, OfflineEarnings.Efficiency(0.5f, 0.05f, 3, 0.75f), 1e-6f);
            Assert.AreEqual(0.75f, OfflineEarnings.Efficiency(0.5f, 0.05f, 20, 0.75f), 1e-6f, "capped");
            Assert.AreEqual(0.8f, OfflineEarnings.Efficiency(0.8f, 0.05f, 2, 0.75f), 1e-6f, "a cap below the base never lowers it");
        }
    }
}
