namespace BeachTycoon.Core
{
    /// <summary>The tutorial moved to Step (equal to the step count when finished).</summary>
    public readonly struct TutorialStepChanged
    {
        public readonly int Step;

        public TutorialStepChanged(int step)
        {
            Step = step;
        }
    }

    /// <summary>The rewarded 2x income boost started or ran out.</summary>
    public readonly struct IncomeBoostChanged
    {
        public readonly bool Active;

        public IncomeBoostChanged(bool active)
        {
            Active = active;
        }
    }

    /// <summary>Sound or music settings changed.</summary>
    public readonly struct SettingsChanged
    {
    }

    /// <summary>The player claimed a daily login reward (Coins already includes any ad multiplier).</summary>
    public readonly struct DailyRewardClaimed
    {
        public readonly int DayIndex;
        public readonly long Coins;
        public readonly float BoostMinutes;

        public DailyRewardClaimed(int dayIndex, long coins, float boostMinutes)
        {
            DayIndex = dayIndex;
            Coins = coins;
            BoostMinutes = boostMinutes;
        }
    }

    /// <summary>The player claimed offline earnings (Amount already includes any ad multiplier).</summary>
    public readonly struct OfflineRewardClaimed
    {
        public readonly long Amount;

        public OfflineRewardClaimed(long amount)
        {
            Amount = amount;
        }
    }
}
