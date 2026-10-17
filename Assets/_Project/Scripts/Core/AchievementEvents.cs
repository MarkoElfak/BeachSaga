namespace BeachTycoon.Core
{
    /// <summary>An achievement's progress changed (index into GameDatabase.achievements).</summary>
    public readonly struct AchievementProgressed
    {
        public readonly int Index;

        public AchievementProgressed(int index)
        {
            Index = index;
        }
    }

    /// <summary>An achievement reached its target; its reward is waiting to be claimed.</summary>
    public readonly struct AchievementCompleted
    {
        public readonly int Index;

        public AchievementCompleted(int index)
        {
            Index = index;
        }
    }

    /// <summary>The player claimed an achievement's reward.</summary>
    public readonly struct AchievementClaimed
    {
        public readonly int Index;
        public readonly long Coins;
        public readonly float BoostMinutes;

        public AchievementClaimed(int index, long coins, float boostMinutes)
        {
            Index = index;
            Coins = coins;
            BoostMinutes = boostMinutes;
        }
    }
}
