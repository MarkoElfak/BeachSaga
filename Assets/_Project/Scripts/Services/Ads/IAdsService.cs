using System;

namespace BeachTycoon.Services
{
    /// <summary>Placements the game asks rewarded ads for.</summary>
    public static class AdPlacements
    {
        public const string IncomeBoost = "income_boost";
        public const string OfflineDouble = "offline_double";
        public const string DailyDouble = "daily_double";
    }

    /// <summary>Rewarded ads seam. The game only ever asks "show one, tell me if it was watched".</summary>
    public interface IAdsService
    {
        bool IsRewardedReady { get; }

        /// <summary>Shows a rewarded ad; onFinished(true) means the reward should be granted.</summary>
        void ShowRewarded(string placement, Action<bool> onFinished);
    }
}
