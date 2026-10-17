using System;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Estimates average coins per second for offline earnings: sales are limited by whichever is lower,
    /// visitor demand (arrivals, capped by maxVisitors / visit length, x purchases per visit) or stand
    /// capacity (1 / serve time per stand); rent is added on top. The season's seashell bonus scales it all.
    /// </summary>
    public sealed class IncomeEstimator
    {
        private const double SecondsPerMinute = 60.0;

        private readonly GameDatabase _database;
        private readonly BuildingManager _buildings;
        private readonly StandService _stands;
        private readonly PopularityService _popularity;
        private readonly NeighbourService _neighbours;
        private readonly SeasonService _season;
        private readonly MiniGameService _miniGames;

        public IncomeEstimator(GameDatabase database, BuildingManager buildings, StandService stands,
            PopularityService popularity, NeighbourService neighbours, SeasonService season = null,
            MiniGameService miniGames = null)
        {
            _season = season;
            _miniGames = miniGames;
            _database = database;
            _buildings = buildings;
            _stands = stands;
            _popularity = popularity;
            _neighbours = neighbours;
        }

        public double IncomePerSecond() => BaseIncomePerSecond() * (_season != null ? _season.IncomeMultiplier : 1f);

        private double BaseIncomePerSecond()
        {
            double rent = _neighbours.Count * _database.neighbours.rentPerMinute / SecondsPerMinute;
            if (_buildings.MainTier <= 0 || _stands.Count == 0)
            {
                return rent;
            }

            VisitorConfig visitors = _database.visitors;
            double purchasesPerVisit = (visitors.purchasesRange.x + visitors.purchasesRange.y) * 0.5;
            double visitorsPerSecond = (1.0 + _popularity.Value) / _database.economy.baseSpawnIntervalSeconds;

            // The beach never holds more than maxVisitors at once, so arrivals are capped by how fast visits end.
            TierDefinition tier = _database.GetTier(_buildings.MainTier);
            double turnover = tier != null ? tier.maxVisitors / (double)visitors.estimatedVisitSeconds : 0.0;
            double demand = Math.Min(visitorsPerSecond, turnover) * purchasesPerVisit;

            float spend = AverageSpendMultiplier(_buildings.MainTier);
            double capacity = 0.0;
            double totalValue = 0.0;
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController stand = _stands.Get(i);
                capacity += 1.0 / Math.Max(0.1f, stand.Product.serveSeconds);
                double value = EconomyMath.SaleValue(stand.Product.price, spend, stand.Model.level,
                    _database.economy.standLevelIncomeBonus);
                totalValue += _miniGames != null ? value * _miniGames.IncomeMultiplier(stand.Product) : value;
            }

            double averageSale = totalValue / _stands.Count;
            return Math.Min(demand, capacity) * averageSale + rent;
        }

        /// <summary>Spawn-weighted spend multiplier of the visitor types available at this tier.</summary>
        private float AverageSpendMultiplier(int tier)
        {
            float weight = 0f, weighted = 0f;
            VisitorTypeDefinition[] types = _database.visitorTypes;
            if (types != null)
            {
                foreach (VisitorTypeDefinition type in types)
                {
                    if (type == null || type.minTier > tier) continue;
                    weight += type.spawnWeight;
                    weighted += type.spawnWeight * type.spendMultiplier;
                }
            }

            return weight > 0f ? weighted / weight : 1f;
        }
    }
}
