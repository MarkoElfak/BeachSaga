using System;

namespace BeachTycoon.Data
{
    /// <summary>Serializable coin balance. Mutated only through EconomyService.</summary>
    [Serializable]
    public sealed class WalletModel
    {
        public long coins;

        public WalletModel()
        {
        }

        public WalletModel(long startingCoins)
        {
            coins = startingCoins;
        }
    }
}
