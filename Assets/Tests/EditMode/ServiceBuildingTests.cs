using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Village buildings (M20): a chapel and a first-aid station help the stands in reach only while someone works
    /// there, weddings pay out, the first-aid station grows into a hospital, a restaurant takes two workers, and
    /// visitors never queue at a service building.
    /// </summary>
    public sealed class ServiceBuildingTests
    {
        private const int Towels = 1;
        private const int Chapel = 2;
        private const int FirstAid = 3;
        private const int Restaurant = 4;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<long> _weddings = new List<long>();
        private GameDatabase _database;
        private StaffConfig _config;
        private GameSaveData _data;
        private BuildingManager _buildings;
        private StandService _stands;
        private PlacementService _placement;
        private WorldNavigation _navigation;
        private CultureService _cultures;
        private CharacterService _characters;
        private StaffService _staff;
        private ServiceBuildings _services;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _database = Asset<GameDatabase>();
            _database.world = Asset<WorldLayoutConfig>();
            _database.terrain = Asset<TerrainConfig>();
            _database.placement = Asset<PlacementConfig>();
            _database.economy = Asset<EconomyConfig>();
            _database.visitors = Asset<VisitorConfig>();
            _database.village = Asset<VillageConfig>();
            _database.palette = Asset<PaletteDefinition>();
            _database.tiers = new[] { Tier(), Tier(), Tier(), Tier(), Tier() };
            _database.products = new[]
            {
                Product("donuts", StaffSkill.Cooking), Product("towels", StaffSkill.Selling),
                Service("chapel", ServiceKind.Weddings, 0.2f), Service("first_aid", ServiceKind.FirstAid, 0.5f),
                Product("restaurant", StaffSkill.Cooking),
            };
            _database.products[Chapel].price = 400;
            _database.products[FirstAid].service.grownTier = 3;
            _database.products[FirstAid].service.grownName = "Hospital";
            _database.products[Restaurant].workerSlots = 2;
            _config = Asset<StaffConfig>();
            _config.paySeconds = 1000f;
            _config.seekerArrivalSeconds = 1000f;
            _config.names = new[] { new WorkerNames { cultureId = "spain", names = new[] { "Ana", "Pablo", "Sofía", "Diego" } } };
            _database.staff = _config;

            var character = Asset<CharacterDefinition>();
            character.id = "lucia";
            character.displayName = "Lucía";
            var spain = Asset<CultureDefinition>();
            spain.id = "spain";
            spain.character = character;
            _database.cultures = new[] { spain };

            _data = GameSaveData.CreateNew(0);
            _data.meta.cultureId = "spain";
            for (int i = 0; i < _database.products.Length; i++)
            {
                _data.stands.Add(new StandModel { productId = _database.products[i].id, lotIndex = 0, slotIndex = i });
            }

            _weddings.Clear();
            EventBus.Subscribe<WeddingHeld>(evt => _weddings.Add(evt.Amount));
        }

        [TearDown]
        public void TearDown()
        {
            _staff?.Dispose();
            _characters?.Dispose();
            _navigation?.Dispose();
            _placement?.Dispose();
            _stands?.Dispose();
            _cultures?.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void Chapel_HelpsTheStandsInReach_OnlyWhileSomeoneWorksThere()
        {
            Create(3);
            StandController chapel = _stands.Get(Chapel);
            _services.Tick(0.1f);
            Assert.AreEqual(0f, _services.Effect(chapel), "nobody there yet");
            Assert.AreEqual(1f, _stands.Get(Towels).SaleBonus);

            StaffMember carer = Hire(Chapel);
            _services.Tick(0.1f);
            float level = 1f + _database.economy.standLevelIncomeBonus * chapel.Model.level;
            float effect = _services.Effect(chapel);
            Assert.AreEqual(0.2f * chapel.ServeSpeed * level, effect, 1e-4f, "amount x the carer's speed x level");
            Assert.Greater(_services.StandsInReach(chapel), 0);
            for (int i = 0; i < _stands.Count; i++)
            {
                StandController stand = _stands.Get(i);
                bool helped = !stand.Product.IsService && _services.InReach(chapel, stand);
                Assert.AreEqual(helped ? 1f + effect : 1f, stand.SaleBonus, 1e-4f, stand.Product.id);
            }

            chapel.Product.service.reach = 0.1f;
            _services.Tick(0.1f);
            Assert.AreEqual(0, _services.StandsInReach(chapel), "too far for everyone");
            Assert.AreEqual(1f, _stands.Get(Towels).SaleBonus);

            chapel.Product.service.reach = 50f;
            Assert.IsTrue(_staff.TryDismiss(carer.Id));
            _services.Tick(0.1f);
            Assert.AreEqual(0f, _services.Effect(chapel), "they left");
            Assert.AreEqual(1f, _stands.Get(Towels).SaleBonus);
        }

        [Test]
        public void Wedding_PaysOut_AfterItsTime()
        {
            Create(3);
            StandController chapel = _stands.Get(Chapel);
            chapel.Product.service.eventSeconds = 10f;
            Assert.IsTrue(float.IsPositiveInfinity(_services.SecondsToWedding(chapel)), "no weddings with nobody there");
            Hire(Chapel);
            _services.Tick(0f);
            float wait = _services.SecondsToWedding(chapel);
            Assert.AreEqual(10f / chapel.ServeSpeed, wait, 1e-3f);

            int sales = 0;
            EventBus.Subscribe<SaleCompleted>(evt => sales += evt.StandIndex == Chapel ? 1 : 0);
            float elapsed = 0f;
            for (int i = 0; i < 400 && _weddings.Count == 0; i++)
            {
                _services.Tick(0.1f);
                elapsed += 0.1f;
            }

            Assert.AreEqual(1, _weddings.Count);
            Assert.AreEqual(wait, elapsed, 0.11f);
            long payout = EconomyMath.SaleValue(400, 1f, chapel.Model.level, _database.economy.standLevelIncomeBonus);
            Assert.AreEqual(payout, _weddings[0]);
            Assert.AreEqual(payout, _services.WeddingPayout(chapel));
            Assert.AreEqual(1, sales, "the coins drop at the chapel like a sale");
        }

        [Test]
        public void FirstAid_LetsVisitorsWaitLonger_AndGrowsIntoAHospital()
        {
            Create(2);
            StandController aid = _stands.Get(FirstAid);
            Assert.AreEqual("first_aid", aid.Product.NameAt(2));
            Hire(FirstAid);
            _services.Tick(0.1f);
            float effect = _services.Effect(aid);
            float reach = _services.Reach(aid);
            Assert.Greater(effect, 0f);
            Assert.AreEqual(1f + effect, _stands.Get(Towels).PatienceMultiplier, 1e-4f);
            Assert.AreEqual(1f, _stands.Get(Towels).SaleBonus, "first aid adds no coins");

            _buildings.GetLot(0).tier = 3;
            _services.Tick(0.1f);
            Assert.IsTrue(aid.Product.IsGrownAt(3));
            Assert.AreEqual("Hospital", aid.Product.NameAt(3));
            Assert.AreEqual(reach * 1.5f, _services.Reach(aid), 1e-4f);
            Assert.AreEqual(effect * 1.5f, _services.Effect(aid), 1e-4f);
            Assert.AreEqual(1f + effect * 1.5f, _stands.Get(Towels).PatienceMultiplier, 1e-4f);
        }

        [Test]
        public void Restaurant_TakesTwoWorkers_WhoServeAsATeam()
        {
            Create(3);
            StaffMember first = Hire(Restaurant);
            Assert.IsTrue(_staff.IsStandFree(Restaurant), "room for one more");
            StaffMember second = Hire(Restaurant);
            Assert.IsFalse(_staff.IsStandFree(Restaurant));
            Assert.IsFalse(_staff.CanHire(_staff.Seekers[0].Id, Restaurant), "full");
            Assert.IsTrue(_staff.CanHire(_staff.Seekers[0].Id, Towels));

            StandController restaurant = _stands.Get(Restaurant);
            float a = StaffMath.ServeSpeed(_config, first.Model, StaffSkill.Cooking, true);
            float b = StaffMath.ServeSpeed(_config, second.Model, StaffSkill.Cooking, true);
            Assert.AreEqual(2, restaurant.People);
            Assert.AreEqual(Mathf.Max(a, b) + Mathf.Min(a, b) * _config.extraWorkerShare, restaurant.ServeSpeed, 1e-4f);
        }

        [Test]
        public void Character_JoinsARestaurantWithRoom_OrSwapsWithItsLastWorker()
        {
            Create(3);
            StaffMember first = Hire(Restaurant);
            Assert.IsTrue(_characters.TrySendTo(Restaurant));
            Walk();
            Assert.AreEqual(Restaurant, first.Model.stand, "room for both");
            Assert.AreEqual(2, _stands.Get(Restaurant).People);

            Assert.IsTrue(_characters.TrySendTo(0));
            Walk();
            StaffMember second = Hire(Restaurant);
            Assert.IsTrue(_characters.TrySendTo(Restaurant));
            Assert.AreEqual(Restaurant, first.Model.stand, "the first one stays");
            Assert.AreEqual(0, second.Model.stand, "the last one takes the character's old place");
            Walk();
            Assert.AreEqual(2, _stands.Get(Restaurant).People);
            Assert.AreEqual(1, _stands.Get(0).People);
        }

        [Test]
        public void Visitors_NeverQueueAtAServiceBuilding()
        {
            Create(2);
            Assert.IsFalse(_stands.Get(Chapel).TryEnqueueOrder(0, 1f));
            var picker = new StandPicker(_stands, new System.Random(3), null);
            var type = Asset<VisitorTypeDefinition>();
            for (int i = 0; i < 200; i++)
            {
                StandController stand = picker.Pick(type);
                Assert.IsNotNull(stand);
                Assert.IsFalse(stand.Product.IsService, stand.Product.id);
            }
        }

        [Test]
        public void ShippedVillageBuildings_HaveTheirTabsSkillsAndChapelStyles()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            ProductDefinition cafe = database.FindProduct("cafe"), restaurant = database.FindProduct("restaurant");
            ProductDefinition aid = database.FindProduct("first_aid"), chapel = database.FindProduct("chapel");
            Assert.IsNotNull(cafe);
            Assert.IsNotNull(restaurant);
            Assert.AreEqual(BuildCategory.Food, cafe.category);
            Assert.AreEqual(2, restaurant.workerSlots);
            Assert.AreEqual(ServiceKind.FirstAid, aid.service.kind);
            Assert.AreEqual(StaffSkill.Care, aid.skill);
            Assert.AreEqual("Hospital", aid.NameAt(5));
            Assert.AreEqual(ServiceKind.Weddings, chapel.service.kind);
            Assert.AreEqual(BuildCategory.Services, chapel.category);
            foreach (ProductDefinition product in database.products)
            {
                if (!string.IsNullOrEmpty(product.cultureId)) Assert.AreEqual(BuildCategory.Food, product.category, product.id);
                if (product.category == BuildCategory.Services) Assert.IsTrue(product.IsService, product.id);
            }

            foreach (CultureDefinition culture in database.cultures)
            {
                GameObject prefab = culture.Resolve(chapel.standPrefab);
                Assert.IsNotNull(prefab, culture.id);
                StringAssert.StartsWith("Chapel", prefab.name, culture.id);
                if (culture.id == "greece") Assert.AreEqual("ChapelGreek", prefab.name);
            }
        }

        private void Create(int tier)
        {
            var economy = new EconomyService(_data.wallet);
            _buildings = new BuildingManager(_database, economy, _data.lots);
            _buildings.GetLot(0).state = LotState.Built;
            _buildings.GetLot(0).tier = tier;
            var boost = new IncomeBoost(Asset<MonetizationConfig>(), _data.meta);
            _stands = new StandService(_database, economy, _buildings, _data.stands, new SaleMultipliers(boost));
            _placement = new PlacementService(_database, _buildings, _data.stands, _data.decorations);
            _navigation = new WorldNavigation(_database.world, _database.visitors, _placement);
            _cultures = new CultureService(_database, _data.meta);
            _characters = new CharacterService(_database, _cultures, _stands, _placement, _navigation, _data.meta);
            _staff = new StaffService(_database, _data, _stands, _buildings, _cultures, _characters, _placement, _navigation, economy);
            _services = new ServiceBuildings(_database, _stands, _placement, _buildings);
        }

        private StaffMember Hire(int stand)
        {
            StaffMember seeker = _staff.Seekers[0];
            Assert.IsTrue(_staff.TryHire(seeker.Id, stand));
            Walk();
            Assert.IsTrue(seeker.IsWorking);
            return seeker;
        }

        private void Walk()
        {
            for (int i = 0; i < 2000 && Walking(); i++)
            {
                _staff.Tick(0.05f);
                _characters.Tick(0.05f);
            }
        }

        private bool Walking()
        {
            if (_characters.IsWalking) return true;
            foreach (StaffMember member in _staff.Staff) if (member.IsWalking) return true;
            return false;
        }

        private TierDefinition Tier()
        {
            var tier = Asset<TierDefinition>();
            tier.footprint = new Rect(-1.6f, -1.55f, 3.2f, 3.1f);
            tier.buildArea = Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f);
            return tier;
        }

        private ProductDefinition Service(string id, ServiceKind kind, float amount)
        {
            ProductDefinition product = Product(id, StaffSkill.Care);
            product.category = BuildCategory.Services;
            product.service = new ServiceEffect { kind = kind, reach = 50f, amount = amount, eventSeconds = 240f };
            return product;
        }

        private ProductDefinition Product(string id, StaffSkill skill)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.displayName = id;
            product.skill = skill;
            product.standFootprint = new Rect(-0.95f, -0.55f, 1.9f, 1.25f);
            product.queueStart = new Vector2(0f, -1.7f);
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
