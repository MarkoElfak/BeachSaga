namespace BeachTycoon.Services
{
    /// <summary>Analytics event names used by the game.</summary>
    public static class AnalyticsEvents
    {
        public const string SessionStart = "session_start";
        public const string TierUpgraded = "tier_upgraded";
        public const string ProductUnlocked = "product_unlocked";
        public const string OfflineRewardClaimed = "offline_reward_claimed";
    }

    /// <summary>Analytics seam. One optional parameter keeps call sites allocation-free.</summary>
    public interface IAnalyticsService
    {
        void LogEvent(string eventName);
        void LogEvent(string eventName, string parameter, long value);
        void LogEvent(string eventName, string parameter, string value);
    }
}
