using System;
using System.Collections.Generic;

namespace BeachTycoon.Data
{
    /// <summary>Per-player state outside the island itself: tutorial, settings, rewards and purchases.</summary>
    [Serializable]
    public sealed class MetaModel
    {
        /// <summary>Index of the current tutorial step; equal to the step count once finished.</summary>
        public int tutorialStep;

        /// <summary>Offline earnings waiting to be claimed (kept in the save so a crash never loses them).</summary>
        public long pendingOfflineCoins;
        public float pendingOfflineSeconds;

        /// <summary>Remaining play-time seconds of the rewarded 2x income boost.</summary>
        public float incomeBoostSeconds;

        public bool removeAdsOwned;

        // Version 6: daily login rewards.
        /// <summary>Consecutive days claimed; the calendar position is (streak - 1) % days.</summary>
        public int dailyStreak;

        /// <summary>Local calendar day of the last claim (yyyy-MM-dd), empty if never claimed.</summary>
        public string dailyLastClaimDay = string.Empty;

        // Version 7: online seams and visiting.
        /// <summary>Local player id (owner id of this island's snapshot), created on first launch.</summary>
        public string playerId = string.Empty;
        public string playerName = string.Empty;

        /// <summary>Local day (yyyy-MM-dd) of visitBonusOwners: islands that already paid a visit bonus that day.</summary>
        public string visitBonusDay = string.Empty;
        public List<string> visitBonusOwners = new List<string>();
        public SettingsModel settings = new SettingsModel();

        // Added with the day and night cycle; older saves start at dawn.
        /// <summary>Position in the day and night cycle (0 dawn, 0.25 midday, 0.5 sunset, 0.75 midnight).</summary>
        public float dayPhase = 0.1f;

        // Version 11: seasons (prestige).
        /// <summary>Seashells collected over all seasons; each one raises all income for good.</summary>
        public long seashells;
        public int seasonsCompleted;
    }

    [Serializable]
    public sealed class SettingsModel
    {
        // Version 3 on/off switches; since version 13 the volumes decide (0 = off) and these are unused.
        public bool soundOn = true;
        public bool musicOn = true;

        // Version 13: volume sliders and vibration.
        public float soundVolume = 1f;
        public float musicVolume = 1f;
        public bool vibrationOn = true;
    }
}
