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
    /// <summary>Hired staff (M18): job seekers, hiring, half speed without anyone, levels, wages, quitting and old managers.</summary>
    public sealed class StaffTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private readonly List<string> _quit = new List<string>();
        private GameDatabase _database;
        private StaffConfig _config;
        private GameSaveData _data;
        private EconomyService _economy;
        private BuildingManager _buildings;
        private StandService _stands;
        private PlacementService _placement;
        private WorldNavigation _navigation;
        private CultureService _cultures;
        private CharacterService _characters;
        private StaffService _staff;
        private int _managers;

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
            _database.tiers = new[] { Tier(), Tier() };
            _database.products = new[]
            {
                Product("donuts", StaffSkill.Cooking), Product("towels", StaffSkill.Selling),
                Product("drinks", StaffSkill.Service), Product("surf", StaffSkill.Sport),
            };
            _config = Asset<StaffConfig>();
            _config.paySeconds = 1000f; // no payday or new seeker while someone walks
            _config.seekerArrivalSeconds = 1000f;
            _config.names = new[] { new WorkerNames { cultureId = "spain", names = new[] { "Ana", "Pablo", "Sofía", "Diego" } } };
            _database.staff = _config;

            var character = Asset<CharacterDefinition>();
            character.id = "lucia";
            character.displayName = "Lucía";
            var spain = Asset<CultureDefinition>();
            spain.id = "spain";
            spain.displayName = "Spain";
            spain.character = character;
            _database.cultures = new[] { spain };

            _data = GameSaveData.CreateNew(0);
            _data.meta.cultureId = "spain";
            for (int i = 0; i < 4; i++) _data.stands.Add(new StandModel { productId = _database.products[i].id, lotIndex = 0, slotIndex = i });
            _quit.Clear();
            _managers = 0;
            EventBus.Subscribe<WorkerQuit>(evt => _quit.Add(evt.Name));
            EventBus.Subscribe<ManagerHired>(_ => _managers++);
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
        public void EmptyStands_ServeAtHalfSpeed_AndJobSeekersWait()
        {
            Create(2);
            Assert.AreEqual(2, _staff.Slots, "the Hut allows two");
            Assert.AreEqual(1.25f, _stands.Get(0).ServeSpeed, 1e-4f, "the character's stand");
            for (int i = 1; i < 4; i++) Assert.AreEqual(0.5f, _stands.Get(i).ServeSpeed, 1e-4f, $"stand {i}");
            Assert.AreEqual(_config.maxJobSeekers, _staff.Seekers.Count);
            Assert.AreEqual(_config.maxJobSeekers, _data.jobSeekers.Count, "saved");
            var names = new HashSet<string>();
            foreach (StaffMember seeker in _staff.Seekers)
            {
                Assert.IsTrue(seeker.IsSeeker);
                Assert.AreEqual("spain", seeker.Model.cultureId);
                Assert.IsTrue(names.Add(seeker.Model.name), "unique names");
                for (int s = 0; s < StaffSkills.Count; s++) Assert.That(seeker.Model.Stars((StaffSkill)s), Is.InRange(1, 3));
            }
        }

        [Test]
        public void NoSlots_InTheTent_UntilTheMainBuildingGrows()
        {
            Create(1);
            Assert.AreEqual(0, _staff.Slots);
            Assert.AreEqual(0, _staff.Seekers.Count);
            Assert.IsFalse(_staff.TryHire(1, 1));

            _buildings.GetLot(0).tier = 2;
            EventBus.Publish(new TierUpgraded(0, 2));
            Assert.AreEqual(_config.maxJobSeekers, _staff.Seekers.Count, "a full set comes to apply");
        }

        [Test]
        public void Hire_WalksOver_ThenServesWithTheSkillBonus()
        {
            Create(2);
            StaffMember seeker = _staff.Seekers[0];
            Assert.IsFalse(_staff.CanHire(seeker.Id, 0), "the character works there");
            Assert.IsTrue(_staff.TryHire(seeker.Id, 1));
            Assert.IsFalse(seeker.IsSeeker);
            Assert.AreEqual(1, seeker.Model.stand);
            CollectionAssert.Contains(_data.staff, seeker.Model);
            CollectionAssert.DoesNotContain(_data.jobSeekers, seeker.Model);
            Assert.IsTrue(seeker.IsWalking);
            Assert.AreEqual(0.5f, _stands.Get(1).ServeSpeed, 1e-4f, "not yet: still walking");

            Walk();
            Assert.IsTrue(seeker.IsWorking);
            float expected = 1f + _config.bonusPerStar * (seeker.Model.Stars(StaffSkill.Selling) - 1) + _config.ownCountryBonus;
            Assert.AreEqual(expected, _stands.Get(1).ServeSpeed, 1e-4f, "towels use Selling; home bonus");
            Assert.IsFalse(_staff.CanHire(_staff.Seekers[0].Id, 1), "one person per stand");
        }

        [Test]
        public void Slots_LimitHiring()
        {
            Create(2);
            Assert.IsTrue(_staff.TryHire(_staff.Seekers[0].Id, 1));
            Assert.IsTrue(_staff.TryHire(_staff.Seekers[0].Id, 2));
            Assert.AreEqual(0, _staff.FreeSlots);
            Assert.IsFalse(_staff.TryHire(_staff.Seekers[0].Id, 3));
        }

        [Test]
        public void CharacterTakingAWorkersStand_SwapsThem()
        {
            Create(2);
            StaffMember worker = Hire(1);
            Assert.IsTrue(_characters.TrySendTo(1));
            Assert.AreEqual(0, worker.Model.stand, "the worker takes the character's old stand");
            Walk();
            Assert.IsTrue(worker.IsWorking);
            Assert.AreEqual(1.25f, _stands.Get(1).ServeSpeed, 1e-4f);
            Assert.Greater(_stands.Get(0).ServeSpeed, 0.9f);
        }

        [Test]
        public void Move_AndDismiss_FreeTheOldStand()
        {
            Create(2);
            StaffMember worker = Hire(1);
            Assert.IsTrue(_staff.TryMove(worker.Id, 3));
            Assert.AreEqual(0.5f, _stands.Get(1).ServeSpeed, 1e-4f);
            Walk();
            Assert.Greater(_stands.Get(3).ServeSpeed, 0.9f);

            Assert.IsTrue(_staff.TryDismiss(worker.Id));
            Assert.AreEqual(0, _staff.Staff.Count);
            Assert.AreEqual(0, _data.staff.Count);
            Assert.AreEqual(0.5f, _stands.Get(3).ServeSpeed, 1e-4f);
        }

        [Test]
        public void Sales_LevelUp_AddAStar_AndLevel3CollectsCoins()
        {
            _config.salesPerLevel = 1;
            Create(2);
            StaffMember worker = Hire(1);
            int stars = worker.Model.Stars(StaffSkill.Selling);
            EventBus.Publish(new SaleCompleted(1, 5, -1));
            Assert.AreEqual(2, worker.Model.level);
            Assert.AreEqual(Mathf.Min(5, stars + 1), worker.Model.Stars(StaffSkill.Selling));
            Assert.IsFalse(_data.stands[1].manager);

            EventBus.Publish(new SaleCompleted(1, 5, -1));
            EventBus.Publish(new SaleCompleted(1, 5, -1));
            Assert.AreEqual(3, worker.Model.level);
            Assert.IsTrue(_data.stands[1].manager, "collects the stand's coins");
            Assert.AreEqual(1, _managers);

            Assert.IsTrue(_staff.TryDismiss(worker.Id));
            Assert.IsFalse(_data.stands[1].manager, "coins wait again once they leave");
        }

        [Test]
        public void Payday_PaysWages_UnpaidWorkersSlowDownThenQuit()
        {
            Create(2);
            StaffMember worker = Hire(1);
            long wage = _staff.Wage(worker.Model);
            Assert.AreEqual(_config.baseWage + _config.wagePerStar * TotalStars(worker.Model), wage);
            _economy.Earn(wage);
            _staff.Tick(_config.paySeconds + 0.1f);
            Assert.AreEqual(0, _economy.Coins, "paid on payday");
            float happy = _stands.Get(1).ServeSpeed;

            _staff.PayWages();
            Assert.AreEqual(1, worker.Model.missedPays);
            Assert.AreEqual(happy * _config.unhappySpeedFactor, _stands.Get(1).ServeSpeed, 1e-4f, "unpaid: slower");
            _staff.PayWages();
            Assert.AreEqual(1, _staff.Staff.Count);
            _staff.PayWages();
            Assert.AreEqual(0, _staff.Staff.Count, "quit after three missed paydays");
            CollectionAssert.AreEqual(new[] { worker.Model.name }, _quit);
            Assert.AreEqual(0.5f, _stands.Get(1).ServeSpeed, 1e-4f);
        }

        [Test]
        public void StaffAndSeekers_SurviveASaveAndLoad()
        {
            Create(2);
            StaffMember worker = Hire(1);
            string json = JsonUtility.ToJson(_data);
            DisposeServices();
            _data = JsonUtility.FromJson<GameSaveData>(json);
            Create(2);
            Assert.AreEqual(1, _staff.Staff.Count);
            Assert.AreEqual(worker.Model.name, _staff.Staff[0].Model.name);
            Assert.IsTrue(_staff.Staff[0].IsWorking, "at their stand on load, no walk");
            Assert.AreEqual(_config.maxJobSeekers - 1, _staff.Seekers.Count);
        }

        [Test]
        public void Version17Managers_BecomeLevel3Workers()
        {
            const string v17 = "{\"version\":17,\"stands\":[{\"productId\":\"donuts\",\"slotIndex\":0},{\"productId\":\"towels\",\"slotIndex\":1,\"manager\":true}]," +
                               "\"meta\":{\"cultureId\":\"spain\",\"characterStand\":0}}";
            _data = JsonUtility.FromJson<GameSaveData>(new SaveMigrator(SaveMigrations.All).MigrateToCurrent(v17, 17));
            Assert.AreEqual(GameSaveData.CurrentVersion, _data.version);
            Assert.AreEqual(1, _data.staff.Count);
            Assert.AreEqual(3, _data.staff[0].level);
            Assert.AreEqual(1, _data.staff[0].stand);

            Create(2);
            WorkerModel model = _staff.Staff[0].Model;
            Assert.IsFalse(string.IsNullOrEmpty(model.name), "named on load");
            Assert.GreaterOrEqual(model.Stars(StaffSkill.Selling), 3);
            Assert.IsTrue(_data.stands[1].manager, "still collects the coins");
        }

        [Test]
        public void NewSeason_StartsTheStaffOver()
        {
            Create(2);
            Hire(1);
            SeasonService.ResetIsland(_data, 100);
            Assert.AreEqual(0, _data.staff.Count);
            Assert.AreEqual(0, _data.jobSeekers.Count);
        }

        [Test]
        public void ShippedStaffConfig_HasNamesForEveryCultureAndAWorkerPrefab()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Assert.IsNotNull(database.staff);
            Assert.IsNotNull(database.staff.workerPrefab);
            Assert.IsNotNull(database.staff.workerPrefab.GetComponent<WorkerView>());
            foreach (CultureDefinition culture in database.cultures)
            {
                WorkerNames names = System.Array.Find(database.staff.names, n => n.cultureId == culture.id);
                Assert.IsNotNull(names, culture.id);
                Assert.GreaterOrEqual(names.names.Length, 6, culture.id);
            }
        }

        private void Create(int tier)
        {
            _economy = new EconomyService(_data.wallet);
            _buildings = new BuildingManager(_database, _economy, _data.lots);
            _buildings.GetLot(0).state = LotState.Built;
            _buildings.GetLot(0).tier = tier;
            var boost = new IncomeBoost(Asset<MonetizationConfig>(), _data.meta);
            _stands = new StandService(_database, _economy, _buildings, _data.stands, new SaleMultipliers(boost));
            _placement = new PlacementService(_database, _buildings, _data.stands, _data.decorations);
            _navigation = new WorldNavigation(_database.world, _database.visitors, _placement);
            _cultures = new CultureService(_database, _data.meta);
            _characters = new CharacterService(_database, _cultures, _stands, _placement, _navigation, _data.meta);
            _staff = new StaffService(_database, _data, _stands, _buildings, _cultures, _characters, _placement, _navigation, _economy);
        }

        private void DisposeServices()
        {
            _staff.Dispose();
            _characters.Dispose();
            _navigation.Dispose();
            _placement.Dispose();
            _stands.Dispose();
            _cultures.Dispose();
            EventBus.ClearAll();
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

        private static int TotalStars(WorkerModel model)
        {
            int stars = 0;
            for (int i = 0; i < StaffSkills.Count; i++) stars += model.Stars((StaffSkill)i);
            return stars;
        }

        private TierDefinition Tier()
        {
            var tier = Asset<TierDefinition>();
            tier.footprint = new Rect(-1.6f, -1.55f, 3.2f, 3.1f);
            tier.buildArea = Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f);
            return tier;
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
