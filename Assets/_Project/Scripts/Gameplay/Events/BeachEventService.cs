using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Runs random timed beach events in play time: waits a random gap, picks an event that can run
    /// (tier reached, its product's stand built) by weight, applies it for its duration, then waits again.
    /// Stands and visitors read the multipliers below; events are not saved, a restart starts a new gap.
    /// </summary>
    public sealed class BeachEventService : ITickable
    {
        private readonly BeachEventConfig _config;
        private readonly BuildingManager _buildings;
        private readonly IAnalyticsService _analytics;
        private readonly System.Random _random;
        private StandService _stands;

        public BeachEventService(BeachEventConfig config, BuildingManager buildings, IAnalyticsService analytics)
        {
            _config = config;
            _buildings = buildings;
            _analytics = analytics;
            _random = new System.Random(config.randomSeed);
            SecondsUntilNext = config.firstEventSeconds;
        }

        /// <summary>The running event, or null.</summary>
        public BeachEventDefinition Active { get; private set; }
        public float RemainingSeconds { get; private set; }
        public float SecondsUntilNext { get; private set; }
        public bool IsActive => Active != null;

        public float SpawnRateMultiplier => IsActive ? Mathf.Max(0.1f, Active.spawnRateMultiplier) : 1f;
        public ProductDefinition DemandProduct => IsActive && Active.demandChance > 0f ? Active.product : null;
        public float DemandChance => DemandProduct != null ? Active.demandChance : 0f;

        public float PriceMultiplier(ProductDefinition product) =>
            IsActive && product != null && Active.product == product ? Active.priceMultiplier : 1f;

        /// <summary>Stands are created after this service (they read its multipliers).</summary>
        public void AttachStands(StandService stands) => _stands = stands;

        public bool CanRun(BeachEventDefinition definition)
        {
            if (definition == null || definition.weight <= 0f || _buildings.MainTier < definition.minTier) return false;
            return definition.product == null || (_stands != null && _stands.FindByProduct(definition.product.id) != null);
        }

        /// <summary>Starts an event now, ending any running one (debug buttons and playtests use this too).</summary>
        public bool Begin(BeachEventDefinition definition)
        {
            if (!CanRun(definition)) return false;
            if (IsActive) End();

            Active = definition;
            RemainingSeconds = definition.durationSeconds;
            _analytics.LogEvent(AnalyticsEvents.BeachEventStarted, "event", definition.id);
            EventBus.Publish(new BeachEventStarted(definition.id));
            EventBus.Publish(new SoundRequested(SoundId.Reward));
            return true;
        }

        /// <summary>Ends the running event and starts the wait for the next one.</summary>
        public void End()
        {
            if (!IsActive) return;
            string id = Active.id;
            Active = null;
            RemainingSeconds = 0f;
            SecondsUntilNext = RandomGap();
            EventBus.Publish(new BeachEventEnded(id));
        }

        public void Tick(float deltaTime)
        {
            if (IsActive)
            {
                RemainingSeconds -= deltaTime;
                if (RemainingSeconds <= 0f) End();
                return;
            }

            SecondsUntilNext -= deltaTime;
            if (SecondsUntilNext > 0f) return;

            BeachEventDefinition next = PickEvent();
            if (next == null || !Begin(next)) SecondsUntilNext = RandomGap(); // nothing can run yet: try later
        }

        private BeachEventDefinition PickEvent()
        {
            BeachEventDefinition[] events = _config.events;
            if (events == null) return null;

            float total = 0f;
            for (int i = 0; i < events.Length; i++)
            {
                if (CanRun(events[i])) total += events[i].weight;
            }

            float roll = (float)_random.NextDouble() * total;
            for (int i = 0; i < events.Length; i++)
            {
                if (!CanRun(events[i])) continue;
                roll -= events[i].weight;
                if (roll <= 0f) return events[i];
            }

            return null;
        }

        private float RandomGap()
        {
            Vector2 range = _config.gapSecondsRange;
            return Mathf.Lerp(range.x, Mathf.Max(range.x, range.y), (float)_random.NextDouble());
        }
    }
}
