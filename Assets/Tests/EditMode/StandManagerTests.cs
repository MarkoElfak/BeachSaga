using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Stands that collect their own coins (run by a level 3 worker) and their offline bonus.</summary>
    public sealed class StandManagerTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private GameDatabase _database;
        private EconomyService _economy;
        private StandService _stands;
        private List<StandModel> _models;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _database = Asset<GameDatabase>();
            _database.economy = Asset<EconomyConfig>();
            _database.world = Asset<WorldLayoutConfig>();
            var donuts = Asset<ProductDefinition>();
            donuts.id = "donuts";
            _database.products = new[] { donuts };
            _database.tiers = new TierDefinition[0];

            _economy = new EconomyService(new WalletModel(0));
            var buildings = new BuildingManager(_database, _economy, new List<LotModel>());
            var boost = new IncomeBoost(Asset<MonetizationConfig>(), new MetaModel());
            _models = new List<StandModel> { new StandModel { productId = "donuts" } };
            _stands = new StandService(_database, _economy, buildings, _models, new SaleMultipliers(boost));
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
