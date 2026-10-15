using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Single source of truth for "how far is the player": the main building's tier and what it unlocks
    /// (products, visitor cap, neighbour slots). Reads the lot models directly so it can be created first.
    /// </summary>
    public sealed class ProgressionService
    {
        private readonly GameDatabase _database;
        private readonly List<LotModel> _lots;

        public ProgressionService(GameDatabase database, List<LotModel> lots)
        {
            _database = database;
            _lots = lots;
        }

        /// <summary>Main building tier, 0 before the tent is built.</summary>
        public int CurrentTier => _lots.Count > BuildingManager.MainLotIndex ? _lots[BuildingManager.MainLotIndex].tier : 0;

        public TierDefinition CurrentTierDefinition => _database.GetTier(CurrentTier);

        public int MaxVisitors => CurrentTierDefinition != null ? CurrentTierDefinition.maxVisitors : 0;

        public int NeighbourSlots => CurrentTierDefinition != null ? CurrentTierDefinition.neighbourSlots : 0;

        public bool IsProductAvailable(ProductDefinition product) => product != null && CurrentTier >= product.requiredTier;

        public bool IsReached(int tier) => CurrentTier >= tier;

        /// <summary>Sum of popularityBonus over every tier reached so far.</summary>
        public float TierPopularity()
        {
            float total = 0f;
            for (int tier = 1; tier <= CurrentTier; tier++)
            {
                TierDefinition definition = _database.GetTier(tier);
                total += definition != null ? definition.popularityBonus : 0f;
            }

            return total;
        }
    }
}
