using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Popularity = sum of reached tiers' popularityBonus + stands * perStand + decoration bonus
    /// - unserved penalty (which decays over time).
    /// </summary>
    public sealed class PopularityService : ITickable, IDisposable
    {
        private const float PublishThreshold = 0.01f;

        private readonly GameDatabase _database;
        private readonly ProgressionService _progression;
        private readonly StandService _stands;
        private float _base;
        private float _decorationBonus;
        private float _penalty;
        private float _lastPublished = -1f;

        public PopularityService(GameDatabase database, ProgressionService progression, StandService stands)
        {
            _database = database;
            _progression = progression;
            _stands = stands;
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<VisitorLeftUnserved>(OnVisitorLeftUnserved);
            Recalculate();
        }

        public float Value => Mathf.Max(0f, _base - _penalty);

        /// <summary>Set by DecorationService whenever decorations change.</summary>
        public void SetDecorationBonus(float bonus)
        {
            _decorationBonus = bonus;
            Recalculate();
        }

        public void Tick(float deltaTime)
        {
            if (_penalty <= 0f)
            {
                return;
            }

            _penalty = Mathf.Max(0f, _penalty - _database.visitors.penaltyDecayPerSecond * deltaTime);
            PublishIfChanged();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<VisitorLeftUnserved>(OnVisitorLeftUnserved);
        }

        private void OnTierUpgraded(TierUpgraded evt) => Recalculate();

        private void OnStandCreated(StandCreated evt) => Recalculate();

        private void OnVisitorLeftUnserved(VisitorLeftUnserved evt)
        {
            VisitorConfig config = _database.visitors;
            _penalty = Mathf.Min(config.maxUnservedPenalty, _penalty + config.unservedPenalty);
            PublishIfChanged();
        }

        private void Recalculate()
        {
            float stands = _stands.Count * _database.visitors.popularityPerStand;
            _base = _progression.TierPopularity() + stands + _decorationBonus;
            PublishIfChanged();
        }

        private void PublishIfChanged()
        {
            float value = Value;
            if (Mathf.Abs(value - _lastPublished) < PublishThreshold)
            {
                return;
            }

            _lastPublished = value;
            EventBus.Publish(new PopularityChanged(value));
        }
    }
}
