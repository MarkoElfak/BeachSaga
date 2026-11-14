using System;

namespace BeachTycoon.Services
{
    /// <summary>Pure economy formulas, kept static and side-effect free so they are easy to unit test.</summary>
    public static class EconomyMath
    {
        /// <summary>Coins for one sale: price * visitor spend multiplier * (1 + level * bonus per level).</summary>
        public static long SaleValue(long price, float spendMultiplier, int standLevel, float levelIncomeBonus)
        {
            double value = price * (double)spendMultiplier * (1.0 + standLevel * (double)levelIncomeBonus);
            return Math.Max(0L, (long)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>Scales an amount (e.g. by the rewarded 2x income boost), rounding to whole coins.</summary>
        public static long ApplyMultiplier(long amount, float multiplier)
        {
            return Math.Max(0L, (long)Math.Round(amount * (double)multiplier, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Visitors the beach holds at once: the tier's room plus room for every open place, scaled by longer stays
        /// (Filoxenia) and rounded up, never more than the pool.
        /// </summary>
        public static int BeachRoom(int tierVisitors, int places, int perPlace, float stayMultiplier, int poolMax)
        {
            double room = (tierVisitors + Math.Max(0, places) * (double)perPlace) * stayMultiplier;
            return (int)Math.Min(poolMax, Math.Ceiling(room - 1e-4)); // 1e-4: float noise in the multiplier
        }

        /// <summary>A count scaled by a multiplier; a roll in [0, 1) settles the fraction (3 x 1.1 is 4 three times in ten).</summary>
        public static int ScaledCount(int count, float multiplier, double roll)
        {
            double scaled = count * (double)multiplier;
            int whole = (int)Math.Floor(scaled + 1e-6);
            return whole + (roll < scaled - whole ? 1 : 0);
        }

        /// <summary>Upgrade cost = base * growth ^ level.</summary>
        public static long UpgradeCost(long baseCost, float growth, int level)
        {
            double value = baseCost * Math.Pow(growth, Math.Max(0, level));
            return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
