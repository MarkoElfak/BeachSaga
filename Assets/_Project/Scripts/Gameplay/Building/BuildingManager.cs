using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>Owns the lot models: starts construction, advances build timers, publishes results.</summary>
    public sealed class BuildingManager : ITickable
    {
        public const int MainLotIndex = 0;
        private const int FirstTier = 1;

        private readonly GameDatabase _database;
        private readonly IEconomyService _economy;
        private readonly List<LotModel> _lots;

        /// <param name="lots">Live lot list from the save; missing lots from the layout are appended.</param>
        public BuildingManager(GameDatabase database, IEconomyService economy, List<LotModel> lots)
        {
            _database = database;
            _economy = economy;
            _lots = lots;
            int configured = database.world.lotPositions != null ? database.world.lotPositions.Length : 0;
            for (int i = _lots.Count; i < configured; i++)
            {
                _lots.Add(new LotModel(i));
            }
        }

        public int LotCount => _lots.Count;

        /// <summary>Tier of the main building (0 before the tent is built).</summary>
        public int MainTier => _lots.Count > MainLotIndex ? _lots[MainLotIndex].tier : 0;

        public LotModel GetLot(int lotIndex) => _lots[lotIndex];

        public static bool IsHouseLot(int lotIndex) => lotIndex != MainLotIndex;

        /// <summary>First empty neighbour lot, or -1 when every house lot is taken.</summary>
        public int FindFreeHouseLot()
        {
            for (int i = 0; i < _lots.Count; i++)
            {
                if (IsHouseLot(i) && _lots[i].state == LotState.Empty) return i;
            }

            return -1;
        }

        /// <summary>Starts a free neighbour house on an empty house lot.</summary>
        public bool TryBuildHouse(int lotIndex, int variant)
        {
            LotModel lot = _lots[lotIndex];
            if (!IsHouseLot(lotIndex) || lot.state != LotState.Empty)
            {
                return false;
            }

            float seconds = _database.neighbours.houseBuildSeconds;
            lot.variant = variant;
            lot.state = LotState.Building;
            lot.targetTier = FirstTier;
            lot.buildTotalSeconds = seconds;
            lot.buildRemainingSeconds = seconds;
            EventBus.Publish(new LotStateChanged(lot.index));
            return true;
        }

        /// <summary>The tier an upgrade of the main building would reach, or null at max / while building / no art yet.</summary>
        public TierDefinition GetNextTier(int lotIndex)
        {
            LotModel lot = _lots[lotIndex];
            if (lot.state == LotState.Building || IsHouseLot(lotIndex))
            {
                return null;
            }

            TierDefinition next = _database.GetTier(lot.tier + 1);
            return next != null && next.buildingPrefab != null ? next : null;
        }

        public bool CanUpgrade(int lotIndex)
        {
            TierDefinition next = GetNextTier(lotIndex);
            return next != null && _economy.CanAfford(next.upgradeCost);
        }

        /// <summary>Builds the tent on the empty main lot (house lots are built by neighbours).</summary>
        public bool TryBuild(int lotIndex)
        {
            LotModel lot = _lots[lotIndex];
            return !IsHouseLot(lotIndex) && lot.state == LotState.Empty && TryStartConstruction(lot, FirstTier);
        }

        public bool TryUpgrade(int lotIndex)
        {
            LotModel lot = _lots[lotIndex];
            return lot.state == LotState.Built && GetNextTier(lotIndex) != null
                && TryStartConstruction(lot, lot.tier + 1);
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _lots.Count; i++)
            {
                LotModel lot = _lots[i];
                if (lot.state != LotState.Building)
                {
                    continue;
                }

                lot.buildRemainingSeconds -= deltaTime;
                if (lot.buildRemainingSeconds <= 0f)
                {
                    CompleteConstruction(lot);
                }
            }
        }

        private bool TryStartConstruction(LotModel lot, int targetTier)
        {
            TierDefinition tier = _database.GetTier(targetTier);
            if (tier == null || !_economy.TrySpend(tier.upgradeCost))
            {
                return false;
            }

            lot.state = LotState.Building;
            lot.targetTier = targetTier;
            lot.buildTotalSeconds = tier.buildSeconds;
            lot.buildRemainingSeconds = tier.buildSeconds;
            EventBus.Publish(new LotStateChanged(lot.index));

            if (tier.buildSeconds <= 0f)
            {
                CompleteConstruction(lot);
            }

            return true;
        }

        private static void CompleteConstruction(LotModel lot)
        {
            lot.state = LotState.Built;
            lot.tier = lot.targetTier;
            lot.buildRemainingSeconds = 0f;
            EventBus.Publish(new LotStateChanged(lot.index));
            EventBus.Publish(new TierUpgraded(lot.index, lot.tier));
        }
    }
}
