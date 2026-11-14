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

    /// <summary>The player answered (or changed) the privacy question.</summary>
    public readonly struct PrivacyChanged
    {
        public readonly bool AnalyticsAllowed;
        public readonly bool PersonalizedAds;

        public PrivacyChanged(bool analyticsAllowed, bool personalizedAds)
        {
            AnalyticsAllowed = analyticsAllowed;
            PersonalizedAds = personalizedAds;
        }
    }

    /// <summary>The village was founded or renamed.</summary>
    public readonly struct VillageRenamed
    {
        public readonly string Name;

        public VillageRenamed(string name)
        {
            Name = name;
        }
    }

    /// <summary>The village's culture was picked (once per game).</summary>
    public readonly struct CultureChosen
    {
        public readonly string CultureId;

        public CultureChosen(string cultureId)
        {
            CultureId = cultureId;
        }
    }

    /// <summary>A country's night-and-events bonus (Spanish Fiesta) started or stopped earning extra.</summary>
    public readonly struct FiestaChanged
    {
        public readonly bool Active;

        public FiestaChanged(bool active)
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
