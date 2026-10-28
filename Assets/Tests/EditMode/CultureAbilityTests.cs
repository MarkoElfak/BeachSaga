using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Country abilities (M19): each country's own place opens only for it, Filoxenia (longer stays), Fiesta (night
    /// and event earnings), Fatto a mano (prices), the offline estimate, friends' islands and the shipped data.
    /// </summary>
    public sealed class CultureAbilityTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        private readonly List<Object> _assets = new List<Object>();
        private readonly List<bool> _fiestas = new List<bool>();
        private GameDatabase _database;
        private MetaModel _meta;
        private CultureService _cultures;
        private DayClock _clock;
        private BeachEventService _events;
        private BeachEventDefinition _bus;
        private CultureAbilities _abilities;
        private SaleMultipliers _sales;
        private StandService _stands;
        private EconomyService _economy;
        private ProductDefinition _donuts, _taverna;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _fiestas.Clear();
            EventBus.Subscribe<FiestaChanged>(evt => _fiestas.Add(evt.Active));
            _database = Asset<GameDatabase>();
            _database.economy = Asset<EconomyConfig>();
            _database.world = Asset<WorldLayoutConfig>();
            _database.tiers = new TierDefinition[0];
            _database.cultures = new[]
            {
                Culture("greece", CultureAbilityKind.LongerStays, 0.2f),
                Culture("spain", CultureAbilityKind.NightAndEvents, 0.25f),
                Culture("italy", CultureAbilityKind.HigherPrices, 0.1f),
            };
            _donuts = Product("donuts", null);
            _taverna = Product("taverna", "greece");
            _database.products = new[] { _donuts, _taverna };

            _meta = new MetaModel();
            _cultures = new CultureService(_database, _meta);
            var dayNight = Asset<DayNightConfig>();
            dayNight.cycleMinutes = 10f;
            _clock = new DayClock(dayNight, _meta);
            _clock.SetPhase(0.25f); // midday
            _economy = new EconomyService(new WalletModel(1000));
            var buildings = new BuildingManager(_database, _economy, new List<LotModel> { new LotModel(0) { state = LotState.Built, tier = 1 } });
            _bus = Asset<BeachEventDefinition>();
            _bus.id = "bus";
            _bus.minTier = 1;
            _bus.durationSeconds = 60f;
            var events = Asset<BeachEventConfig>();
            events.firstEventSeconds = 1000f;
            events.events = new[] { _bus };
            _events = new BeachEventService(events, buildings, new MockAnalyticsService(false));
            _abilities = new CultureAbilities(_cultures, _clock, _events);
            _sales = new SaleMultipliers(new IncomeBoost(Asset<MonetizationConfig>(), new MetaModel()), null, _events, null, _abilities);
            _stands = new StandService(_database, _economy, buildings, new List<StandModel>(), _sales, _cultures);
            _events.AttachStands(_stands);
        }

        [TearDown]
        public void TearDown()
        {
            _stands?.Dispose();
            _cultures?.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void Place_OpensOnlyForItsOwnCountry()
        {
            Assert.IsFalse(_stands.IsAvailable(_taverna), "no country yet");
            Assert.IsTrue(_stands.IsAvailable(_donuts));

            _meta.cultureId = "spain";
            Assert.IsFalse(_stands.CanUnlock(_taverna));
            Assert.IsFalse(_stands.TryUnlockProduct(_taverna), "a Spanish village can't open the Greek taverna");
            Assert.AreEqual(1000, _economy.Coins, "nothing was paid");

            _meta.cultureId = "greece";
            Assert.IsTrue(_stands.TryUnlockProduct(_taverna));
            Assert.AreSame(_taverna, _cultures.PlaceOf(_cultures.Current));
            Assert.IsNull(_cultures.PlaceOf(_cultures.Find("italy")), "Italy has no place in this data");
        }

        [Test]
        public void StandsWithoutCultures_KeepEveryPlaceClosed()
        {
            var stands = new StandService(_database, _economy, new BuildingManager(_database, _economy, new List<LotModel>()), new List<StandModel>(), _sales);
            Assert.IsFalse(stands.IsAvailable(_taverna));
            Assert.IsTrue(stands.IsAvailable(_donuts));
            stands.Dispose();
        }

        [Test]
        public void FattoAMano_RaisesEverySale()
        {
            Assert.AreEqual(1000, _sales.Apply(1000, _donuts), "no country, no bonus");
            _meta.cultureId = "italy";
            Assert.AreEqual(1100, _sales.Apply(1000, _donuts));
            _clock.SetPhase(0.7f);
            Assert.AreEqual(1100, _sales.Apply(1000, _donuts), "day or night");
            Assert.AreEqual(1.1f, _abilities.AveragePriceMultiplier, 1e-4f);
        }

        [Test]
        public void Fiesta_PaysMoreAtNightAndDuringEvents()
        {
            _meta.cultureId = "spain";
            Assert.AreEqual(1000, _sales.Apply(1000, _donuts), "midday");
            Assert.IsFalse(_abilities.IsFiesta);

            _clock.SetPhase(0.7f);
            Assert.IsTrue(_abilities.IsFiesta);
            Assert.AreEqual(1250, _sales.Apply(1000, _donuts), "night");
            _abilities.Tick(0.1f);
            CollectionAssert.AreEqual(new[] { true }, _fiestas, "the Fiesta is announced once");
            _abilities.Tick(0.1f);
            Assert.AreEqual(1, _fiestas.Count);

            _clock.SetPhase(0.25f);
            Assert.IsTrue(_events.Begin(_bus));
            Assert.AreEqual(1250, _sales.Apply(1000, _donuts), "a beach event by day");
            _events.End();
            _abilities.Tick(0.1f);
            CollectionAssert.AreEqual(new[] { true, false }, _fiestas);
            Assert.AreEqual(1f + 0.25f * DayClock.NightShare, _abilities.AveragePriceMultiplier, 1e-4f, "offline: only the night counts");
        }

        [Test]
        public void Filoxenia_MakesVisitorsStayLonger_AndOthersDoNot()
        {
            Assert.AreEqual(1f, _abilities.StayMultiplier);
            _meta.cultureId = "greece";
            Assert.AreEqual(1.2f, _abilities.StayMultiplier, 1e-4f);
            Assert.AreEqual(1000, _sales.Apply(1000, _donuts), "Filoxenia does not change prices");
            Assert.AreEqual(1f, _abilities.AveragePriceMultiplier);
            _meta.cultureId = "italy";
            Assert.AreEqual(1f, _abilities.StayMultiplier);
        }

        [Test]
        public void FriendsIslands_ShowOnlyTheirOwnPlace()
        {
            using (var data = new OnlineTestData())
            {
                data.Database.cultures = new[] { Culture("greece", CultureAbilityKind.LongerStays, 0.2f), Culture("spain", CultureAbilityKind.NightAndEvents, 0.25f) };
                data.AddProduct("taverna", 2, "greece");
                data.AddProduct("tapas_bar", 2, "spain");

                var demo = new DemoIsland { ownerId = "demo", displayName = "Demo", cultureId = "spain", tier = 3 };
                IslandSnapshot snapshot = DemoIslands.Create(data.Database, demo, Now);
                List<string> ids = snapshot.stands.ConvertAll(stand => stand.productId);
                CollectionAssert.Contains(ids, "tapas_bar");
                CollectionAssert.DoesNotContain(ids, "taverna");

                // A tampered snapshot can't show another country's place.
                snapshot.stands.Add(new SnapshotStand { productId = "taverna", x = 30f, z = 0f });
                List<string> clean = IslandSnapshotValidator.Sanitize(snapshot, data.Database).stands.ConvertAll(stand => stand.productId);
                CollectionAssert.Contains(clean, "tapas_bar");
                CollectionAssert.DoesNotContain(clean, "taverna");
            }
        }

        [Test]
        public void ShippedCountries_HaveAnAbilityAndTheirOwnPlace()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var shipped = new CultureService(database, new MetaModel());

            ProductDefinition first = null;
            foreach (CultureDefinition culture in database.cultures)
            {
                Assert.AreNotEqual(CultureAbilityKind.None, culture.ability.kind, culture.id);
                Assert.IsNotEmpty(culture.ability.name, culture.id);
                ProductDefinition place = shipped.PlaceOf(culture);
                Assert.IsNotNull(place, $"{culture.id} has its own place");
                Assert.IsNotNull(place.standPrefab.GetComponent<StandView>(), place.id);
                Assert.IsNotEmpty(place.description, place.id);
                first = first ?? place;
                Assert.AreEqual(first.price, place.price, $"{place.id}: every country's place earns the same");
                Assert.AreEqual(first.requiredTier, place.requiredTier, place.id);
            }

            string[] shared = { "donuts", "towels", "drinks", "surf_lessons", "houses" };
            for (int i = 0; i < shared.Length; i++) Assert.AreEqual(shared[i], database.products[i].id, "the shared products keep their places in the build bar");
            shipped.Dispose();
        }

        private CultureDefinition Culture(string id, CultureAbilityKind kind, float amount)
        {
            var culture = Asset<CultureDefinition>();
            culture.id = id;
            culture.ability = new CultureAbility { name = id + " bonus", kind = kind, amount = amount };
            return culture;
        }

        private ProductDefinition Product(string id, string cultureId)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.cultureId = cultureId;
            product.unlockCost = 100;
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
