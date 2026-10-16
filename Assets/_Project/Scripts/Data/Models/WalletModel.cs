using System;

namespace BeachTycoon.Data
{
    /// <summary>Serializable coin balance. Mutated only through EconomyService.</summary>
    [Serializable]
    public sealed class WalletModel
    {
        public long coins;

        /// <summary>Coins earned since the season started (version 11); sets the seashells a new season pays.</summary>
        public long seasonEarned;

        public WalletModel()
        {
        }

        public WalletModel(long startingCoins)
        {
            coins = startingCoins;
        }
    }
}
