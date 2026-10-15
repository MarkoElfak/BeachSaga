using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>Buys decorations, places them on the next free decoration spot and feeds their popularity bonus.</summary>
    public sealed class DecorationService
    {
        private readonly GameDatabase _database;
        private readonly IEconomyService _economy;
        private readonly ProgressionService _progression;
        private readonly PopularityService _popularity;
        private readonly List<DecorationModel> _placed;

        public DecorationService(GameDatabase database, IEconomyService economy, ProgressionService progression,
            PopularityService popularity, List<DecorationModel> placed)
        {
            _database = database;
            _economy = economy;
            _progression = progression;
            _popularity = popularity;
            _placed = placed;
            UpdatePopularity();
        }

        public int Count => _placed.Count;
        public DecorationModel Get(int index) => _placed[index];

        public int CountOf(DecorationDefinition definition)
        {
            int count = 0;
            for (int i = 0; i < _placed.Count; i++)
            {
                if (_placed[i].decorationId == definition.id) count++;
            }

            return count;
        }

        public bool IsUnlocked(DecorationDefinition definition) => _progression.IsReached(definition.requiredTier);

        public bool IsSoldOut(DecorationDefinition definition) => CountOf(definition) >= definition.maxCount || FreeSpot() < 0;

        public bool CanBuy(DecorationDefinition definition) =>
            IsUnlocked(definition) && !IsSoldOut(definition) && _economy.CanAfford(definition.cost);

        public bool TryBuy(DecorationDefinition definition)
        {
            int spot = FreeSpot();
            if (!IsUnlocked(definition) || IsSoldOut(definition) || !_economy.TrySpend(definition.cost))
            {
                return false;
            }

            _placed.Add(new DecorationModel { decorationId = definition.id, spotIndex = spot });
            UpdatePopularity();
            EventBus.Publish(new DecorationPlaced(_placed.Count - 1));
            return true;
        }

        private int FreeSpot()
        {
            int spots = _database.world.decorationSpots != null ? _database.world.decorationSpots.Length : 0;
            for (int spot = 0; spot < spots; spot++)
            {
                bool taken = false;
                for (int i = 0; i < _placed.Count && !taken; i++) taken = _placed[i].spotIndex == spot;
                if (!taken) return spot;
            }

            return -1;
        }

        private void UpdatePopularity()
        {
            float bonus = 0f;
            for (int i = 0; i < _placed.Count; i++)
            {
                DecorationDefinition definition = _database.FindDecoration(_placed[i].decorationId);
                bonus += _database.visitors.popularityPerDecoration + (definition != null ? definition.popularity : 0f);
            }

            _popularity.SetDecorationBonus(bonus);
        }
    }
}
