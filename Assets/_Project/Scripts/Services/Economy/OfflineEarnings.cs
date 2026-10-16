using System;

namespace BeachTycoon.Services
{
    /// <summary>Pure offline-earnings maths: income rate x capped absence x efficiency.</summary>
    public static class OfflineEarnings
    {
        private const double SecondsPerHour = 3600.0;

        /// <summary>Seconds that count for offline earnings: the absence capped at capHours, or 0 below minSeconds.</summary>
        public static double CappedSeconds(TimeSpan absence, float capHours, float minSeconds)
        {
            double seconds = absence.TotalSeconds;
            if (seconds < minSeconds || seconds <= 0.0)
            {
                return 0.0;
            }

            return Math.Min(seconds, capHours * SecondsPerHour);
        }

        public static long Calculate(double incomePerSecond, TimeSpan absence, float efficiency, float capHours, float minSeconds)
        {
            double seconds = CappedSeconds(absence, capHours, minSeconds);
            double coins = Math.Max(0.0, incomePerSecond) * seconds * efficiency;
            return (long)Math.Floor(coins);
        }

        /// <summary>Base efficiency plus a bonus per hired manager, capped (never below the base).</summary>
        public static float Efficiency(float baseEfficiency, float perManager, int managers, float max)
        {
            float boosted = Math.Min(max, baseEfficiency + perManager * Math.Max(0, managers));
            return Math.Max(baseEfficiency, boosted);
        }
    }
}
