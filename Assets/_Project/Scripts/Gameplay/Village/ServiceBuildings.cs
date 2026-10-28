using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Service buildings (M20): while someone works at a chapel or a first-aid station, every stand within its reach
    /// gets its effect. A chapel's guests pay more there, it adds popularity and a wedding pays out now and then; a
    /// first-aid station lets visitors wait longer in line, and does more once it grows into a hospital. The effect
    /// follows whoever works there (their speed, as at a stand) and the building's level; with nobody there, none.
    /// </summary>
    public sealed class ServiceBuildings : ITickable
    {
        private readonly StandService _stands;
        private readonly PlacementService _placement;
        private readonly BuildingManager _buildings;
        private readonly EconomyConfig _economy;
        private readonly PopularityService _popularity;
        private float[] _weddingTimers = new float[0];
        private float _popularityBonus;

        public ServiceBuildings(GameDatabase database, StandService stands, PlacementService placement, BuildingManager buildings,
            PopularityService popularity = null)
        {
            _stands = stands;
            _placement = placement;
            _buildings = buildings;
            _economy = database.economy;
            _popularity = popularity;
        }

        /// <summary>How far the building reaches (further once grown into a hospital).</summary>
        public float Reach(StandController building) => building.Product.service.reach * Grown(building);

        /// <summary>
        /// Its effect on each stand in reach now (extra coins per sale, or extra patience): the building's amount
        /// scaled by the speed of the people working there and its level. 0 with nobody there.
        /// </summary>
        public float Effect(StandController building)
        {
            if (!building.Product.IsService || building.People <= 0) return 0f;
            float level = 1f + _economy.standLevelIncomeBonus * building.Model.level;
            return building.Product.service.amount * building.ServeSpeed * level * Grown(building);
        }

        public bool InReach(StandController building, StandController stand)
        {
            Vector2 from = Centre(building.Index), to = Centre(stand.Index);
            return (to - from).sqrMagnitude <= Reach(building) * Reach(building);
        }

        /// <summary>Stands (not other service buildings) within the building's reach.</summary>
        public int StandsInReach(StandController building)
        {
            int count = 0;
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController stand = _stands.Get(i);
                if (!stand.Product.IsService && InReach(building, stand)) count++;
            }

            return count;
        }

        /// <summary>Coins a wedding at this chapel pays (more with each level).</summary>
        public long WeddingPayout(StandController chapel) =>
            EconomyMath.SaleValue(chapel.Product.price, 1f, chapel.Model.level, _economy.standLevelIncomeBonus);

        /// <summary>Seconds left until the next wedding at this chapel at the current speed (infinite with nobody there).</summary>
        public float SecondsToWedding(StandController chapel)
        {
            if (chapel.People <= 0) return float.PositiveInfinity;
            float timer = chapel.Index < _weddingTimers.Length ? _weddingTimers[chapel.Index] : 0f;
            return Mathf.Max(0f, chapel.Product.service.eventSeconds - timer) / Mathf.Max(0.01f, chapel.ServeSpeed);
        }

        public void Tick(float deltaTime)
        {
            if (_weddingTimers.Length < _stands.Count) Array.Resize(ref _weddingTimers, _stands.Count); // only when a stand opens
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController stand = _stands.Get(i);
                stand.SaleBonus = 1f;
                stand.PatienceMultiplier = 1f;
            }

            float popularity = 0f;
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController building = _stands.Get(i);
                float effect = Effect(building);
                if (effect <= 0f) continue;
                popularity += building.Product.service.popularity;
                Spread(building, effect);
                if (building.Product.service.kind == ServiceKind.Weddings) TickWedding(building, deltaTime);
            }

            if (_popularity != null && !Mathf.Approximately(popularity, _popularityBonus)) _popularity.SetServiceBonus(popularity);
            _popularityBonus = popularity;
        }

        private void Spread(StandController building, float effect)
        {
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController stand = _stands.Get(i);
                if (stand.Product.IsService || !InReach(building, stand)) continue;
                if (building.Product.service.kind == ServiceKind.Weddings) stand.SaleBonus += effect;
                else stand.PatienceMultiplier += effect;
            }
        }

        // Faster with better people at the chapel; the coins drop there like a sale (and train whoever works there).
        private void TickWedding(StandController chapel, float deltaTime)
        {
            _weddingTimers[chapel.Index] += deltaTime * chapel.ServeSpeed;
            if (_weddingTimers[chapel.Index] < chapel.Product.service.eventSeconds) return;
            _weddingTimers[chapel.Index] = 0f;
            long amount = WeddingPayout(chapel);
            EventBus.Publish(new SaleCompleted(chapel.Index, amount, -1));
            EventBus.Publish(new WeddingHeld(chapel.Index, amount));
        }

        private float Grown(StandController building) =>
            building.Product.IsGrownAt(_buildings.MainTier) ? building.Product.service.grownScale : 1f;

        private Vector2 Centre(int standIndex)
        {
            var id = PlaceableId.Stand(standIndex);
            return _placement.GetFootprint(id, _placement.GetStandPosition(standIndex)).center;
        }
    }
}
