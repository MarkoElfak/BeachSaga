using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.EditorTools;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Economy balance (M21): the beach's room grows with every place and with Filoxenia, Filoxenia visitors buy
    /// more, the balance model follows the visitor and stand rules, and the shipped numbers keep every tier and
    /// every new place paying for itself in reasonable time.
    /// </summary>
    public sealed class EconomyBalanceTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private GameDatabase _database;
        private ProductDefinition _cheap;
        private ProductDefinition _dear;

        [SetUp]
        public void SetUp()
        {
            _database = Asset<GameDatabase>();
            _database.economy = Asset<EconomyConfig>();
            _database.visitors = Asset<VisitorConfig>();
            _database.neighbours = Asset<NeighbourConfig>();
            _database.staff = Asset<StaffConfig>();
            var tier = Asset<TierDefinition>();
            tier.displayName = "Tent";
            tier.maxVisitors = 2;
            _database.tiers = new[] { tier };
            _cheap = Product("cheap", 10, 0);
            _dear = Product("dear", 20, 100);
            _database.products = new[] { _cheap, _dear };
            var visitor = Asset<VisitorTypeDefinition>();
            visitor.preferredProducts = new[] { _cheap };
            _database.visitorTypes = new[] { visitor };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void BeachRoom_GrowsWithPlacesAndLongerStays_WithinThePool()
        {
            Assert.AreEqual(2, EconomyMath.BeachRoom(2, 0, 2, 1f, 120));
            Assert.AreEqual(12, EconomyMath.BeachRoom(6, 3, 2, 1f, 120), "6 + 3 places x 2");
            Assert.AreEqual(14, EconomyMath.BeachRoom(6, 3, 2, 1.1f, 120), "13.2 rounds up");
            Assert.AreEqual(11, EconomyMath.BeachRoom(10, 0, 2, 1.1f, 120), "no float noise: 10 x 1.1 is 11");
            Assert.AreEqual(120, EconomyMath.BeachRoom(100, 11, 2, 1.1f, 120), "never past the pool");
        }

        [Test]
        public void ScaledCount_RollsTheFraction()
        {
            Assert.AreEqual(3, EconomyMath.ScaledCount(3, 1f, 0.0));
            Assert.AreEqual(4, EconomyMath.ScaledCount(3, 1.1f, 0.29), "3.3: one more three times in ten");
            Assert.AreEqual(3, EconomyMath.ScaledCount(3, 1.1f, 0.31));
            Assert.AreEqual(3, EconomyMath.ScaledCount(2, 1.5f, 0.99));
        }

        [Test]
        public void Model_SendsHalfTheFavouritesHome_AndSpreadsTheRest()
        {
            var model = new EconomyModel(_database, null);
            EconomyResult result = model.Run(1, Places(), new List<ProductDefinition>());

            // Room 2 + 2 places x 2 = 6 over an 80 s visit: 0.075 visitors a second, 2.5 purchases each.
            Assert.AreEqual(270.0, result.Visitors, 1e-6);
            // Half to the favourite (cheap), the rest evenly: cheap 0.140625/s at 10, dear 0.046875/s at 20.
            Assert.AreEqual(2.34375 * 3600.0, result.Sales, 1e-6);
            Assert.AreEqual(0.0, result.Unserved, 1e-9);
            Assert.AreEqual(result.Sales, result.Net, 1e-6, "no staff, rent or weddings");
        }

        [Test]
        public void Model_SlowStands_LeaveVisitorsUnserved_AndStaffHelp()
        {
            _cheap.serveSeconds = 20f;
            _dear.serveSeconds = 20f;
            var model = new EconomyModel(_database, null);
            EconomyResult alone = model.Run(1, Places(), new List<ProductDefinition>());
            Assert.AreEqual((0.1875 - 0.05) * 3600.0, alone.Unserved, 1e-6, "two stands at half speed serve 0.05 a second");

            EconomyResult staffed = model.Run(1, Places(), new List<ProductDefinition> { _dear });
            Assert.Greater(staffed.Sales, alone.Sales);
            Assert.AreEqual(model.WagePerHour, staffed.Wages, 1e-6);
            Assert.AreEqual(30.0 * 60.0, model.WagePerHour, 1e-6, "10 + 2 x 10 stars, every minute");
        }

        [Test]
        public void Model_Filoxenia_EarnsMore_AndFattoAMano_RaisesPricesOnly()
        {
            double plain = new EconomyModel(_database, null).Run(1, Places(), new List<ProductDefinition>()).Net;
            double greek = new EconomyModel(_database, Culture(CultureAbilityKind.LongerStays, 0.1f)).Run(1, Places(), new List<ProductDefinition>()).Net;
            double italian = new EconomyModel(_database, Culture(CultureAbilityKind.HigherPrices, 0.1f)).Run(1, Places(), new List<ProductDefinition>()).Net;
            Assert.Greater(greek, plain * 1.1, "room for 7 instead of 6, and 10% more purchases each");
            Assert.AreEqual(plain * 1.1, italian, 1e-3);
        }

        [Test]
        public void Shipped_EveryTierPaysForTheNext_AndEveryNewPlacePaysBack()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var nets = new List<double>[database.cultures.Length];
            for (int c = 0; c < database.cultures.Length; c++)
            {
                List<TierBalance> rows = new EconomyBalance(database, database.cultures[c]).Build();
                nets[c] = new List<double>();
                for (int i = 0; i < rows.Count; i++)
                {
                    TierBalance row = rows[i];
                    nets[c].Add(row.Result.Net);
                    if (i > 0) Assert.Greater(row.Result.Net, rows[i - 1].Result.Net, $"{row.Name} earns more than the tier before");
                    Assert.Less(row.MinutesToNext, 60.0, $"{row.Name}: its places and the next tier within an hour");
                    foreach (PlaceBalance place in row.NewPlaces)
                    {
                        if (place.Product.unlockCost > 0) Assert.Less(place.PaybackMinutes, 20.0, $"{place.Product.displayName} pays back");
                    }
                }
            }

            for (int i = 0; i < nets[0].Count; i++)
            {
                for (int c = 1; c < nets.Length; c++)
                {
                    Assert.AreEqual(1.0, nets[c][i] / nets[0][i], 0.15, $"{database.cultures[c].displayName} within 15% of {database.cultures[0].displayName} at tier {i + 1}");
                }
            }
        }

        private List<ProductDefinition> Places() => new List<ProductDefinition> { _cheap, _dear };

        private CultureDefinition Culture(CultureAbilityKind kind, float amount)
        {
            var culture = Asset<CultureDefinition>();
            culture.id = "test";
            culture.ability = new CultureAbility { kind = kind, amount = amount };
            return culture;
        }

        private ProductDefinition Product(string id, long price, long unlock)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.displayName = id;
            product.price = price;
            product.unlockCost = unlock;
            product.serveSeconds = 1f;
            product.requiredTier = 1;
            return product;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
