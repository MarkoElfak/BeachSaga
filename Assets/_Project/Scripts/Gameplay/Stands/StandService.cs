using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Owns every stand: restores them from the save, creates free products when a tier is reached,
    /// unlocks paid products, upgrades stand levels and ticks serving.
    /// </summary>
    public sealed class StandService : ITickable, IDisposable
    {
        private readonly GameDatabase _database;
        private readonly IEconomyService _economy;
        private readonly BuildingManager _buildings;
        private readonly List<StandModel> _models;
        private readonly List<StandController> _stands = new List<StandController>();

        public StandService(GameDatabase database, IEconomyService economy, BuildingManager buildings,
            List<StandModel> savedStands)
        {
            _database = database;
            _economy = economy;
            _buildings = buildings;
            _models = savedStands;
            foreach (StandModel model in _models)
            {
                ProductDefinition product = database.FindProduct(model.productId);
                if (product != null)
                {
                    AddController(model, product);
                }
            }

            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
        }

        public int Count => _stands.Count;

        public StandController Get(int standIndex) => _stands[standIndex];

        public StandController FindByProduct(string productId)
        {
            for (int i = 0; i < _stands.Count; i++)
            {
                if (_stands[i].Model.productId == productId)
                {
                    return _stands[i];
                }
            }

            return null;
        }

        public bool IsTierReached(ProductDefinition product) => _buildings.MainTier >= product.requiredTier;

        public bool CanUnlock(ProductDefinition product) =>
            FindByProduct(product.id) == null && IsTierReached(product) && _economy.CanAfford(product.unlockCost);

        public bool TryUnlockProduct(ProductDefinition product)
        {
            if (FindByProduct(product.id) != null || !IsTierReached(product) || !_economy.TrySpend(product.unlockCost))
            {
                return false;
            }

            CreateStand(product, BuildingManager.MainLotIndex);
            EventBus.Publish(new ProductUnlocked(product.id));
            return true;
        }

        public long GetUpgradeCost(StandController stand) =>
            EconomyMath.UpgradeCost(stand.Product.upgradeBaseCost, _database.economy.upgradeCostGrowth, stand.Model.level);

        public bool IsMaxLevel(StandController stand) => stand.Model.level >= _database.economy.maxStandLevel;

        public bool TryUpgradeStand(StandController stand)
        {
            if (IsMaxLevel(stand) || !_economy.TrySpend(GetUpgradeCost(stand)))
            {
                return false;
            }

            stand.Model.level++;
            EventBus.Publish(new StandUpgraded(stand.Index, stand.Model.level));
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _stands.Count; i++)
            {
                _stands[i].Tick(deltaTime);
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
        }

        private void OnTierUpgraded(TierUpgraded evt)
        {
            TierDefinition tier = _database.GetTier(evt.Tier);
            if (evt.LotIndex != BuildingManager.MainLotIndex || tier == null || tier.unlockedProducts == null)
            {
                return;
            }

            foreach (ProductDefinition product in tier.unlockedProducts)
            {
                bool isFree = product != null && product.unlockCost == 0;
                if (isFree && product.requiredTier <= evt.Tier && FindByProduct(product.id) == null)
                {
                    CreateStand(product, evt.LotIndex);
                }
            }
        }

        private void CreateStand(ProductDefinition product, int lotIndex)
        {
            var model = new StandModel
            {
                productId = product.id,
                lotIndex = lotIndex,
                slotIndex = CountStandsOnLot(lotIndex),
            };

            _models.Add(model);
            StandController stand = AddController(model, product);
            EventBus.Publish(new StandCreated(stand.Index, lotIndex));
        }

        private StandController AddController(StandModel model, ProductDefinition product)
        {
            var stand = new StandController(_stands.Count, model, product, _database.economy);
            _stands.Add(stand);
            return stand;
        }

        private int CountStandsOnLot(int lotIndex)
        {
            int count = 0;
            for (int i = 0; i < _stands.Count; i++)
            {
                if (_stands[i].Model.lotIndex == lotIndex)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
