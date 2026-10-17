using System;
using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>Wallet logic on top of a serializable WalletModel.</summary>
    public sealed class EconomyService : IEconomyService
    {
        private readonly WalletModel _wallet;

        public EconomyService(WalletModel wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public long Coins => _wallet.coins;

        public bool CanAfford(long amount)
        {
            return amount <= _wallet.coins;
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0 || !CanAfford(amount))
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            _wallet.coins -= amount;
            EventBus.Publish(new CoinsChanged(_wallet.coins, -amount));
            return true;
        }

        public void Earn(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _wallet.coins += amount;
            _wallet.seasonEarned += amount;
            EventBus.Publish(new CoinsChanged(_wallet.coins, amount));
        }
    }
}
