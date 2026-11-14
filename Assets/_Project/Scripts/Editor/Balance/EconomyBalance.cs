using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.EditorTools
{
    /// <summary>One tier of the balance table: the beach with every place up to it open and its staff hired.</summary>
    public sealed class TierBalance
    {
        public int Tier;
        public string Name;
        public EconomyResult Result;
        public readonly List<ProductDefinition> Staffed = new List<ProductDefinition>();
        public readonly List<PlaceBalance> NewPlaces = new List<PlaceBalance>();
        /// <summary>Opening this tier's new places plus building the next tier.</summary>
        public long CostToNext;
        public double MinutesToNext;
    }

    /// <summary>A place that opens at a tier: what it adds to the beach and how soon it pays for itself.</summary>
    public sealed class PlaceBalance
    {
        public ProductDefinition Product;
        public double SalesPerHour;
        public double AddsPerHour;
        public double PaybackMinutes;
    }

    /// <summary>
    /// The balance table for one country: at every tier, every place open to the village is open, your character
    /// works at the first stand and the staff slots go to the places where a typical worker adds the most (and more
    /// than their wage). A place's payback is
    /// its cost over what the beach earns with it minus what it earns without it.
    /// </summary>
    public sealed class EconomyBalance
    {
        private readonly GameDatabase _db;
        private readonly CultureDefinition _culture;

        public EconomyBalance(GameDatabase database, CultureDefinition culture)
        {
            _db = database;
            _culture = culture;
            Model = new EconomyModel(database, culture);
        }

        public EconomyModel Model { get; }

        public List<TierBalance> Build()
        {
            var rows = new List<TierBalance>();
            for (int tier = 1; tier <= _db.MaxTier; tier++) rows.Add(Row(tier));
            return rows;
        }

        public TierBalance Row(int tier)
        {
            List<ProductDefinition> places = Places(tier);
            var row = new TierBalance { Tier = tier, Name = _db.GetTier(tier).displayName };
            row.Result = Best(tier, places, row.Staffed);
            long open = 0;
            foreach (ProductDefinition place in places)
            {
                if (place.requiredTier != tier) continue;
                open += place.unlockCost;
                Model.Run(tier, places, row.Staffed);
                row.NewPlaces.Add(Payback(tier, places, place, row.Result.Net));
            }

            TierDefinition next = _db.GetTier(tier + 1);
            row.CostToNext = open + (next != null ? next.upgradeCost : 0);
            row.MinutesToNext = row.Result.Net > 0.0 ? row.CostToNext / row.Result.Net * 60.0 : double.PositiveInfinity;
            return row;
        }

        /// <summary>Every place this village can open by this tier (each country only its own place).</summary>
        public List<ProductDefinition> Places(int tier)
        {
            var places = new List<ProductDefinition>();
            string cultureId = _culture != null ? _culture.id : string.Empty;
            foreach (ProductDefinition product in _db.products)
            {
                if (product != null && product.requiredTier <= tier && product.IsOpenTo(cultureId)) places.Add(product);
            }

            return places;
        }

        /// <summary>Hires one worker at a time where they add the most, while that beats their wage.</summary>
        public EconomyResult Best(int tier, List<ProductDefinition> places, List<ProductDefinition> staffed)
        {
            staffed.Clear();
            EconomyResult best = Model.Run(tier, places, staffed);
            int slots = _db.staff.SlotsFor(tier);
            while (staffed.Count < slots)
            {
                ProductDefinition pick = null;
                EconomyResult picked = best;
                foreach (ProductDefinition place in places)
                {
                    if (Count(staffed, place) >= Model.StaffSeats(place)) continue;
                    staffed.Add(place);
                    EconomyResult result = Model.Run(tier, places, staffed);
                    staffed.RemoveAt(staffed.Count - 1);
                    if (result.Net > picked.Net + 0.5) { pick = place; picked = result; }
                }

                if (pick == null) break;
                staffed.Add(pick);
                best = picked;
            }

            Model.Run(tier, places, staffed); // leaves the per-place sales of the chosen beach
            return best;
        }

        private PlaceBalance Payback(int tier, List<ProductDefinition> places, ProductDefinition place, double net)
        {
            var row = new PlaceBalance { Product = place, SalesPerHour = Model.SalesOf(place) };
            var without = new List<ProductDefinition>(places);
            without.Remove(place);
            row.AddsPerHour = net - Best(tier, without, new List<ProductDefinition>()).Net;
            row.PaybackMinutes = row.AddsPerHour > 0.0 ? place.unlockCost / row.AddsPerHour * 60.0 : double.PositiveInfinity;
            return row;
        }

        private static int Count(List<ProductDefinition> list, ProductDefinition item)
        {
            int count = 0;
            foreach (ProductDefinition entry in list) if (entry == item) count++;
            return count;
        }
    }
}
