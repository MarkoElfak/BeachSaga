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

        /// <summary>Upgrade cost = base * growth ^ level.</summary>
        public static long UpgradeCost(long baseCost, float growth, int level)
        {
            double value = baseCost * Math.Pow(growth, Math.Max(0, level));
            return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
