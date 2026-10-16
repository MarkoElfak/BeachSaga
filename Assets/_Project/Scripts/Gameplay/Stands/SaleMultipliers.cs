using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Everything that scales a stand sale, applied in order: the rewarded 2x boost, the season's seashell
    /// bonus and a running beach event's price for that product. Season and events are optional (tests).
    /// </summary>
    public sealed class SaleMultipliers
    {
        private readonly IncomeBoost _boost;
        private readonly SeasonService _season;
        private readonly BeachEventService _events;

        public SaleMultipliers(IncomeBoost boost, SeasonService season = null, BeachEventService events = null)
        {
            _boost = boost;
            _season = season;
            _events = events;
        }

        public long Apply(long amount, ProductDefinition product)
        {
            amount = EconomyMath.ApplyMultiplier(amount, _boost.Multiplier);
            if (_season != null) amount = EconomyMath.ApplyMultiplier(amount, _season.IncomeMultiplier);
            if (_events != null) amount = EconomyMath.ApplyMultiplier(amount, _events.PriceMultiplier(product));
            return amount;
        }
    }
}
