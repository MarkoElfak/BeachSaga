namespace BeachTycoon.Services
{
    /// <summary>Analytics event names used by the game.</summary>
    public static class AnalyticsEvents
    {
        public const string SessionStart = "session_start";
        public const string TierUpgraded = "tier_upgraded";
        public const string ProductUnlocked = "product_unlocked";
        public const string OfflineRewardClaimed = "offline_reward_claimed";
        public const string DailyRewardClaimed = "daily_reward_claimed";
        public const string IslandVisited = "island_visited";
        public const string IslandLiked = "island_liked";
        public const string GiftSent = "gift_sent";
        public const string AchievementClaimed = "achievement_claimed";
        public const string ManagerHired = "manager_hired";
    }

    /// <summary>Analytics seam. One optional parameter keeps call sites allocation-free.</summary>
    public interface IAnalyticsService
    {
        void LogEvent(string eventName);
        void LogEvent(string eventName, string parameter, long value);
        void LogEvent(string eventName, string parameter, string value);
    }
}
