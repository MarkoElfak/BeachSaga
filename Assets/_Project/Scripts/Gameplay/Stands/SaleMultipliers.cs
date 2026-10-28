using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Everything that scales a stand sale, applied in order: the rewarded 2x boost, the season's seashell
    /// bonus, the product's mini-game bonus, a running beach event's price for that product and the country's
    /// ability (higher prices, or a Fiesta). All but the boost are optional (tests).
    /// </summary>
    public sealed class SaleMultipliers
    {
        private readonly IncomeBoost _boost;
        private readonly SeasonService _season;
        private readonly BeachEventService _events;
        private readonly MiniGameService _miniGames;
        private readonly CultureAbilities _abilities;

        public SaleMultipliers(IncomeBoost boost, SeasonService season = null, BeachEventService events = null,
            MiniGameService miniGames = null, CultureAbilities abilities = null)
        {
            _abilities = abilities;
            _boost = boost;
            _season = season;
            _events = events;
            _miniGames = miniGames;
        }

        public long Apply(long amount, ProductDefinition product)
        {
            amount = EconomyMath.ApplyMultiplier(amount, _boost.Multiplier);
            if (_season != null) amount = EconomyMath.ApplyMultiplier(amount, _season.IncomeMultiplier);
            if (_miniGames != null) amount = EconomyMath.ApplyMultiplier(amount, _miniGames.IncomeMultiplier(product));
            if (_events != null) amount = EconomyMath.ApplyMultiplier(amount, _events.PriceMultiplier(product));
            if (_abilities != null) amount = EconomyMath.ApplyMultiplier(amount, _abilities.PriceMultiplier);
            return amount;
        }
    }
}
