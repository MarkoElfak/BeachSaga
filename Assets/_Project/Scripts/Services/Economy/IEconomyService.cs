namespace BeachTycoon.Services
{
    /// <summary>Owns the wallet. All coin changes go through here so CoinsChanged is always published.</summary>
    public interface IEconomyService
    {
        long Coins { get; }
        bool CanAfford(long amount);
        bool TrySpend(long amount);
        void Earn(long amount);
    }
}
