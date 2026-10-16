using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Buys decorations, places them on the first free default spot (or the nearest free ground when the
    /// player has built over the spots) and feeds their popularity bonus.
    /// </summary>
    public sealed class DecorationService
    {
        private readonly GameDatabase _database;
        private readonly IEconomyService _economy;
        private readonly ProgressionService _progression;
        private readonly PopularityService _popularity;
        private readonly List<DecorationModel> _placed;
        private PlacementService _placement;

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

        /// <summary>Placement is created after this service (it needs the decoration list), so it is attached later.</summary>
        public void AttachPlacement(PlacementService placement) => _placement = placement;
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

        public bool IsSoldOut(DecorationDefinition definition) => CountOf(definition) >= definition.maxCount;

        public bool CanBuy(DecorationDefinition definition) =>
            IsUnlocked(definition) && !IsSoldOut(definition) && _economy.CanAfford(definition.cost);

        public bool TryBuy(DecorationDefinition definition)
        {
            if (!IsUnlocked(definition) || IsSoldOut(definition) || !_economy.CanAfford(definition.cost)
                || !TryChoosePosition(definition, out int spot, out Vector2 position))
            {
                return false;
            }

            _economy.TrySpend(definition.cost);
            _placed.Add(new DecorationModel
            {
                decorationId = definition.id, spotIndex = spot, placed = true, x = position.x, z = position.y,
            });
            UpdatePopularity();
            EventBus.Publish(new DecorationPlaced(_placed.Count - 1));
            return true;
        }

        private bool TryChoosePosition(DecorationDefinition definition, out int spot, out Vector2 position)
        {
            Vector2[] spots = _database.world.decorationSpots ?? new Vector2[0];
            for (spot = 0; spot < spots.Length; spot++)
            {
                position = spots[spot];
                if (_placement == null || _placement.CanPlaceNewDecoration(definition, position)) return true;
            }

            spot = 0;
            Vector2 near = spots.Length > 0 ? spots[0] : Vector2.zero;
            if (_placement != null && _placement.TryFindFreeSpotForNewDecoration(definition, near, out position)) return true;
            position = near;
            return false;
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
