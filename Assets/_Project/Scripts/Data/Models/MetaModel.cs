using System;

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
        public SettingsModel settings = new SettingsModel();
    }

    [Serializable]
    public sealed class SettingsModel
    {
        public bool soundOn = true;
        public bool musicOn = true;
    }
}
