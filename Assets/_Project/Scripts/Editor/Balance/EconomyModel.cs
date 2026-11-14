using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The balance model behind the economy report: a beach at one tier with some places open and some of them
    /// staffed, in steady play. Arrivals follow popularity, capped by the beach's room over the visit length (as
    /// offline earnings); a purchase goes to the visitor's favourite stand some of the time, otherwise to any stand
    /// with room; a stand serves no faster than its people allow (your character works at the first stand for free).
    /// Places at level 0, no boosts, events or seasons.
    /// </summary>
    public sealed partial class EconomyModel
    {
        private const double Hour = 3600.0;
        /// <summary>The share of stands a chapel reaches in a typical village.</summary>
        public const float ChapelReachShare = 0.5f;

        private readonly GameDatabase _db;
        private readonly CultureAbility _ability;
        private readonly Dictionary<ProductDefinition, double> _capacity = new Dictionary<ProductDefinition, double>();
        private readonly Dictionary<ProductDefinition, double> _load = new Dictionary<ProductDefinition, double>();
        private readonly Dictionary<ProductDefinition, double> _value = new Dictionary<ProductDefinition, double>();

        public EconomyModel(GameDatabase database, CultureDefinition culture)
        {
            _db = database;
            _ability = culture != null && culture.ability != null ? culture.ability : new CultureAbility();
            StaffConfig staff = database.staff;
            int stars = (staff.newSkillStars.x + staff.newSkillStars.y) / 2;
            TypicalWorker = new WorkerModel { cultureId = culture != null ? culture.id : string.Empty };
            for (int i = 0; i < TypicalWorker.skills.Length; i++) TypicalWorker.skills[i] = stars;
            CharacterSpeed = culture != null && culture.character != null ? 1f + culture.character.serveSpeedBonus : 0f;
            foreach (ProductDefinition product in database.products)
            {
                if (product != null && product.requiredTier == 1 && product.unlockCost == 0) { CharacterPlace = product; break; }
            }
        }

        /// <summary>The free first stand, where your character starts work (0 speed: no character).</summary>
        public ProductDefinition CharacterPlace { get; }
        public float CharacterSpeed { get; }

        /// <summary>Seats left for staff (the character takes one at their stand).</summary>
        public int StaffSeats(ProductDefinition place) => place.workerSlots - (HasCharacter(place) ? 1 : 0);

        /// <summary>A new hire: the middle of the new-skill stars in every skill, from the village's own country.</summary>
        public WorkerModel TypicalWorker { get; }

        public float WorkerSpeed => StaffMath.ServeSpeed(_db.staff, TypicalWorker, StaffSkill.Cooking, true);
        public double WagePerHour => StaffMath.Wage(_db.staff, TypicalWorker) * Hour / _db.staff.paySeconds;

        /// <summary>Filoxenia: visits last longer, each buys more and the beach holds more.</summary>
        public float Stay => _ability.kind == CultureAbilityKind.LongerStays ? 1f + _ability.amount : 1f;

        /// <summary>Higher prices, or a Fiesta counted for the night only (as offline earnings).</summary>
        public float PriceMultiplier
        {
            get
            {
                if (_ability.kind == CultureAbilityKind.HigherPrices) return 1f + _ability.amount;
                if (_ability.kind == CultureAbilityKind.NightAndEvents) return 1f + _ability.amount * DayClock.NightShare;
                return 1f;
            }
        }

        /// <summary>One hour at this tier; 'staffed' lists a place once per person working there.</summary>
        public EconomyResult Run(int tier, IList<ProductDefinition> places, IList<ProductDefinition> staffed)
        {
            VisitorConfig visitors = _db.visitors;
            TierDefinition definition = _db.GetTier(tier);
            float chapel = ChapelEffect(staffed, out double weddings, out float popularity);
            double arrivals = (1.0 + TierPopularity(tier) + places.Count * visitors.popularityPerStand + popularity) / _db.economy.baseSpawnIntervalSeconds;
            int room = EconomyMath.BeachRoom(definition.maxVisitors, places.Count, visitors.visitorsPerPlace, Stay, visitors.poolMaxSize);
            double perSecond = Math.Min(arrivals, room / (visitors.estimatedVisitSeconds * (double)Stay));
            double demand = perSecond * (visitors.purchasesRange.x + visitors.purchasesRange.y) * 0.5 * Stay;
            double unserved = Share(tier, places, staffed, demand);

            double sales = 0.0;
            foreach (KeyValuePair<ProductDefinition, double> pair in _value)
            {
                sales += pair.Value * pair.Key.price * PriceMultiplier * (1.0 + chapel * ChapelReachShare);
            }

            return new EconomyResult
            {
                Visitors = perSecond * Hour,
                Sales = sales * Hour,
                Weddings = weddings * Hour,
                Rent = definition.neighbourSlots * _db.neighbours.rentPerMinute * 60.0,
                Wages = staffed.Count * WagePerHour,
                Unserved = unserved * Hour,
            };
        }

        /// <summary>Coins per hour a place sold for in the last Run (before the chapel and country bonuses).</summary>
        public double SalesOf(ProductDefinition product) =>
            _value.TryGetValue(product, out double value) ? value * product.price * Hour : 0.0;

        private float SpeedOf(ProductDefinition place, IList<ProductDefinition> staffed)
        {
            int people = Count(staffed, place);
            bool character = HasCharacter(place);
            if (people == 0 && !character) return _db.staff.unstaffedServeSpeed;
            float fastest = character ? Math.Max(CharacterSpeed, people > 0 ? WorkerSpeed : 0f) : WorkerSpeed;
            float total = WorkerSpeed * people + (character ? CharacterSpeed : 0f);
            return StaffMath.TeamSpeed(_db.staff, fastest, total - fastest);
        }

        private bool HasCharacter(ProductDefinition place) => place == CharacterPlace && CharacterSpeed > 0f;

        // A staffed chapel: extra coins per sale in reach, weddings and popularity.
        private float ChapelEffect(IList<ProductDefinition> staffed, out double weddings, out float popularity)
        {
            float effect = 0f;
            weddings = 0.0;
            popularity = 0f;
            foreach (ProductDefinition place in staffed)
            {
                if (place.service.kind != ServiceKind.Weddings) continue;
                effect += place.service.amount * WorkerSpeed;
                weddings += place.price * WorkerSpeed / place.service.eventSeconds;
                popularity += place.service.popularity;
            }

            return effect;
        }

        private float TierPopularity(int tier)
        {
            float total = 0f;
            for (int i = 1; i <= tier; i++) total += _db.GetTier(i).popularityBonus;
            return total;
        }

        private static int Count(IList<ProductDefinition> list, ProductDefinition item)
        {
            int count = 0;
            foreach (ProductDefinition entry in list) if (entry == item) count++;
            return count;
        }
    }
}
