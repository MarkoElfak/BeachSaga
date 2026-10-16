using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// What an achievement measures. Counters add up from events; levels (tier, neighbours, decorations,
    /// stand level, login streak) track the best value reached, so old saves get credit right away.
    /// </summary>
    public enum AchievementGoal
    {
        SellProduct = 0,
        EarnCoins = 1,
        ServeVisitors = 2,
        CompleteQuests = 3,
        VisitIslands = 4,
        ReachTier = 10,
        HostNeighbours = 11,
        OwnDecorations = 12,
        StandLevel = 13,
        DailyStreak = 14,
    }

    /// <summary>A long-term goal with a one-time reward, e.g. "Sell 100 donuts".</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Achievement", fileName = "Achievement")]
    public sealed class AchievementDefinition : ScriptableObject
    {
        public string id;
        public string title = "Achievement";

        [Tooltip("{0} = target amount, {1} = product name.")]
        public string description = "Sell {0} {1}";

        public AchievementGoal goal;
        [Tooltip("Only for SellProduct; empty counts every product.")]
        public ProductDefinition product;
        [Min(1)] public long target = 10;
        [Min(0)] public long rewardCoins = 100;
        [Min(0f)] public float rewardBoostMinutes;

        public bool IsLevelGoal => (int)goal >= (int)AchievementGoal.ReachTier;
    }
}
