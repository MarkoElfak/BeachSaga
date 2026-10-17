using System;

namespace BeachTycoon.Services
{
    /// <summary>New season (prestige) formulas: seashells from a season's earnings and the income bonus they give.</summary>
    public static class SeasonMath
    {
        /// <summary>floor((earned / unit) ^ exponent); nothing for an empty season.</summary>
        public static long Seashells(long seasonEarned, long coinUnit, float exponent)
        {
            if (seasonEarned <= 0 || coinUnit <= 0) return 0;
            double shells = Math.Pow(seasonEarned / (double)coinUnit, exponent);
            return (long)Math.Floor(shells + 1e-9); // exact powers (e.g. 4 units -> 2) never round down to 1
        }

        /// <summary>1 + seashells x bonus per seashell (1.0 = no bonus).</summary>
        public static float IncomeMultiplier(long seashells, float bonusPerSeashell) =>
            1f + Math.Max(0L, seashells) * Math.Max(0f, bonusPerSeashell);
    }
}
