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
    /// <summary>The player's character: who joins, where they work, walking to another stand and the faster serving.</summary>
    public sealed class CharacterTests
    {
        private const float Boosted = 1.25f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<int> _assigned = new List<int>();
        private readonly List<int> _started = new List<int>();
        private GameDatabase _database;
        private MetaModel _meta;
        private List<StandModel> _models;
        private StandService _stands;
        private PlacementService _placement;
        private WorldNavigation _navigation;
        private CultureService _cultures;
        private CharacterService _characters;
        private int _joined;

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
            var tier = Asset<TierDefinition>();
            tier.footprint = new Rect(-1.6f, -1.55f, 3.2f, 3.1f);
            tier.buildArea = Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f);
            _database.tiers = new[] { tier };
            _database.products = new[] { Product("donuts"), Product("towels") };

            var character = Asset<CharacterDefinition>();
            character.id = "lucia";
            character.displayName = "Lucía";
            var spain = Asset<CultureDefinition>();
            spain.id = "spain";
            spain.character = character;
            _database.cultures = new[] { spain };

            _meta = new MetaModel { villageName = "Sol Bay", cultureId = "spain" };
            var economy = new EconomyService(new WalletModel(0));
            var buildings = new BuildingManager(_database, economy, new List<LotModel>());
            buildings.GetLot(0).state = LotState.Built;
            buildings.GetLot(0).tier = 1;
            _models = new List<StandModel>
            {
                new StandModel { productId = "donuts", lotIndex = 0, slotIndex = 0 },
                new StandModel { productId = "towels", lotIndex = 0, slotIndex = 1 },
            };
            var boost = new IncomeBoost(Asset<MonetizationConfig>(), new MetaModel());
            _stands = new StandService(_database, economy, buildings, _models, new SaleMultipliers(boost));
            _placement = new PlacementService(_database, buildings, _models, new List<DecorationModel>());
            _navigation = new WorldNavigation(_database.world, _database.visitors, _placement);
            _cultures = new CultureService(_database, _meta);

            _assigned.Clear();
            _started.Clear();
            _joined = 0;
            EventBus.Subscribe<CharacterJoined>(_ => _joined++);
            EventBus.Subscribe<CharacterAssigned>(evt => _assigned.Add(evt.StandIndex));
            EventBus.Subscribe<CharacterStartedWork>(evt => _started.Add(evt.StandIndex));
        }

        [TearDown]
        public void TearDown()
        {
            _characters?.Dispose();
            _navigation.Dispose();
            _placement.Dispose();
            _stands.Dispose();
            _cultures.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void SaveFromBeforeCharacters_StartsAtTheFirstStand_WhichServesFaster()
        {
            CreateCharacters();
            Assert.IsTrue(_characters.HasJoined);
            Assert.AreEqual("Lucía", _characters.Character.displayName, "the culture's character");
            Assert.AreEqual(0, _characters.StandIndex);
            Assert.IsTrue(_characters.IsWorking, "already at work, no walk on load");
            Assert.IsFalse(_characters.IsWalking);
            Assert.AreEqual(Boosted, _stands.Get(0).ServeSpeed);
            Assert.AreEqual(1f, _stands.Get(1).ServeSpeed);
            Assert.AreEqual(1, _joined);
            Vector2 stand = _placement.GetStandPosition(0);
            Assert.Less(Vector3.Distance(_characters.Agent.Position, new Vector3(stand.x, 0f, stand.y)), 2f, "beside the stand");
        }

        [Test]
        public void NoCharacter_UntilACultureIsChosen()
        {
            _meta.cultureId = string.Empty;
            CreateCharacters();
            Assert.IsFalse(_characters.HasJoined);
            Assert.AreEqual(1f, _stands.Get(0).ServeSpeed);

            _meta.cultureId = "spain";
            EventBus.Publish(new CultureChosen("spain"));
            Assert.IsTrue(_characters.HasJoined);
            Assert.AreEqual(1, _joined);
            Assert.AreEqual(Boosted, _stands.Get(0).ServeSpeed);
        }

        [Test]
        public void SendTo_WalksOver_ThenTheBonusMovesWithThem()
        {
            CreateCharacters();
            Assert.IsFalse(_characters.CanWorkAt(0), "already works there");
            Assert.IsFalse(_characters.CanWorkAt(2), "no such stand");
            Assert.IsTrue(_characters.TrySendTo(1));
            Assert.AreEqual(1, _meta.characterStand, "saved");
            Assert.IsTrue(_characters.IsWalking);
            Assert.IsFalse(_characters.IsWorking);
            Assert.AreEqual(1f, _stands.Get(0).ServeSpeed, "the old stand is back to normal");
            Assert.AreEqual(1f, _stands.Get(1).ServeSpeed, "not yet: still walking");
            CollectionAssert.AreEqual(new[] { 1 }, _assigned);

            for (int i = 0; i < 600 && _characters.IsWalking; i++) _characters.Tick(0.05f);
            Assert.IsTrue(_characters.IsWorking);
            Assert.AreEqual(Boosted, _stands.Get(1).ServeSpeed);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _started);
        }

        [Test]
        public void ServeSpeed_FinishesOrdersSooner()
        {
            StandController stand = _stands.Get(0);
            stand.Product.serveSeconds = 2f;
            int sales = 0;
            EventBus.Subscribe<SaleCompleted>(_ => sales++);
            stand.ServeSpeed = 2f;
            Assert.IsTrue(stand.TryEnqueueOrder(7, 1f));
            stand.Tick(0.9f);
            Assert.AreEqual(0, sales);
            stand.Tick(0.2f);
            Assert.AreEqual(1, sales, "two-second order done in one second");
        }

        [Test]
        public void Portrait_HasBackgroundFaceAndShirt()
        {
            var character = Asset<CharacterDefinition>();
            character.portraitBackground = Color.cyan;
            Color32[] pixels = PortraitPainter.Paint(character, _database.palette, 64);
            Assert.AreEqual((Color32)Color.cyan, pixels[63 * 64], "top-left corner");
            Assert.AreEqual((Color32)Color.magenta, pixels[28 * 64 + 32], "face (unknown palette keys draw magenta)");
            Assert.AreEqual((Color32)Color.magenta, pixels[2 * 64 + 32], "shirt");
        }

        [Test]
        public void Version16Save_MigratesWithNoStandRecorded()
        {
            const string v16 = "{\"version\":16,\"wallet\":{\"coins\":9},\"meta\":{\"cultureId\":\"italy\"}}";
            var data = JsonUtility.FromJson<GameSaveData>(new SaveMigrator(SaveMigrations.All).MigrateToCurrent(v16, 16));
            Assert.AreEqual(GameSaveData.CurrentVersion, data.version);
            Assert.AreEqual("italy", data.meta.cultureId);
            Assert.AreEqual(-1, data.meta.characterStand);
        }

        [Test]
        public void ShippedCultures_EachHaveTheirCharacter()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            string[] names = { "Nikos", "Lucía", "Marco" };
            Assert.AreEqual(names.Length, database.cultures.Length);
            for (int i = 0; i < names.Length; i++)
            {
                CharacterDefinition character = database.cultures[i].character;
                Assert.IsNotNull(character, database.cultures[i].id);
                Assert.AreEqual(names[i], character.displayName);
                Assert.IsNotNull(character.prefab, names[i] + " has a prefab");
                Assert.IsNotNull(character.prefab.GetComponent<CharacterView>());
                Assert.IsFalse(string.IsNullOrEmpty(character.tagline) || string.IsNullOrEmpty(character.ability));
                Assert.AreEqual(0.25f, character.serveSpeedBonus, 1e-4f);
            }
        }

        private void CreateCharacters() =>
            _characters = new CharacterService(_database, _cultures, _stands, _placement, _navigation, _meta);

        private ProductDefinition Product(string id)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.displayName = id;
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
