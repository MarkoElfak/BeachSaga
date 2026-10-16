using System;
using System.Globalization;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Pure daily-login maths on calendar days. One claim per day; claiming the day after the last claim
    /// continues the streak, a longer gap starts it again at day 1, and the reward cycle repeats after the
    /// last day. A clock moved backwards never allows a second claim.
    /// </summary>
    public static class DailyRewardCalendar
    {
        private const string DayFormat = "yyyy-MM-dd";
        private const double SecondsPerMinute = 60.0;

        public static string FormatDay(DateTime day) => day.Date.ToString(DayFormat, CultureInfo.InvariantCulture);

        /// <summary>Parses a saved day; empty or unreadable values mean "never claimed".</summary>
        public static DateTime? ParseDay(string value)
        {
            bool parsed = DateTime.TryParseExact(value, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime day);
            return parsed ? day.Date : (DateTime?)null;
        }

        public static bool CanClaim(DateTime? lastClaimDay, DateTime today)
        {
            return !lastClaimDay.HasValue || today.Date > lastClaimDay.Value.Date;
        }

        /// <summary>True when claiming today keeps the streak going (the last claim was yesterday).</summary>
        public static bool ContinuesStreak(DateTime? lastClaimDay, DateTime today)
        {
            return lastClaimDay.HasValue && (today.Date - lastClaimDay.Value.Date).Days == 1;
        }

        /// <summary>Streak length right now: the saved streak while it is still alive, otherwise 0.</summary>
        public static int CurrentStreak(DateTime? lastClaimDay, int streak, DateTime today)
        {
            bool alive = !CanClaim(lastClaimDay, today) || ContinuesStreak(lastClaimDay, today);
            return alive ? Math.Max(0, streak) : 0;
        }

        /// <summary>Streak length after claiming today.</summary>
        public static int StreakAfterClaim(DateTime? lastClaimDay, int streak, DateTime today)
        {
            return ContinuesStreak(lastClaimDay, today) ? Math.Max(0, streak) + 1 : 1;
        }

        /// <summary>
        /// Cycle position (0-based) of today's reward: the one a claim would pay now, or the one already
        /// claimed today.
        /// </summary>
        public static int DayIndex(DateTime? lastClaimDay, int streak, DateTime today, int cycleLength)
        {
            if (cycleLength <= 0)
            {
                return 0;
            }

            int claims = CanClaim(lastClaimDay, today) ? StreakAfterClaim(lastClaimDay, streak, today) : streak;
            return (Math.Max(1, claims) - 1) % cycleLength;
        }

        /// <summary>Coins for a day: minutes of income, floored, but never below the minimum.</summary>
        public static long Coins(double incomePerSecond, float incomeMinutes, long minimumCoins)
        {
            double income = Math.Max(0.0, incomePerSecond) * Math.Max(0f, incomeMinutes) * SecondsPerMinute;
            return Math.Max(Math.Max(0L, minimumCoins), (long)Math.Floor(income));
        }
    }
}
