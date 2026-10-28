using System;
using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.EditorTools
{
    /// <summary>How the balance model shares the beach's purchases out among its stands.</summary>
    public sealed partial class EconomyModel
    {
        // Favourites first (their share of each type's purchases), then the rest spread evenly over stands with room.
        private double Share(int tier, IList<ProductDefinition> places, IList<ProductDefinition> staffed, double demand)
        {
            _capacity.Clear();
            _load.Clear();
            _value.Clear();
            foreach (ProductDefinition place in places)
            {
                if (place.IsService) continue;
                _capacity[place] = SpeedOf(place, staffed) / Math.Max(0.1f, place.serveSeconds);
                _load[place] = 0.0;
                _value[place] = 0.0;
            }

            double weight = 0.0, rest = 0.0, restSpend = 0.0;
            foreach (VisitorTypeDefinition type in _db.visitorTypes) if (Eligible(type, tier)) weight += type.spawnWeight;
            foreach (VisitorTypeDefinition type in _db.visitorTypes)
            {
                if (!Eligible(type, tier)) continue;
                double wanted = demand * type.spawnWeight / weight;
                ProductDefinition favourite = Favourite(type);
                if (favourite != null)
                {
                    double taken = Math.Min(wanted * _db.visitors.preferredChance, _capacity[favourite] - _load[favourite]);
                    _load[favourite] += taken;
                    _value[favourite] += taken * type.spendMultiplier;
                    wanted -= taken;
                }

                rest += wanted;
                restSpend += wanted * type.spendMultiplier;
            }

            return Spread(rest, rest > 0.0 ? restSpend / rest : 1.0);
        }

        // Water filling: an even share to every stand with room; what a full stand can't take goes round again.
        private double Spread(double rest, double spend)
        {
            var open = new List<ProductDefinition>(_capacity.Keys);
            while (rest > 1e-9 && open.Count > 0)
            {
                double share = rest / open.Count;
                for (int i = open.Count - 1; i >= 0; i--)
                {
                    ProductDefinition place = open[i];
                    double taken = Math.Min(share, _capacity[place] - _load[place]);
                    _load[place] += taken;
                    _value[place] += taken * spend;
                    rest -= taken;
                    if (_capacity[place] - _load[place] <= 1e-9) open.RemoveAt(i);
                }
            }

            return Math.Max(0.0, rest);
        }

        private ProductDefinition Favourite(VisitorTypeDefinition type)
        {
            if (type.preferredProducts == null) return null;
            foreach (ProductDefinition product in type.preferredProducts)
            {
                if (product != null && _capacity.ContainsKey(product)) return product;
            }

            return null;
        }

        private static bool Eligible(VisitorTypeDefinition type, int tier) => type != null && type.minTier <= tier && type.spawnWeight > 0f;
    }
}
