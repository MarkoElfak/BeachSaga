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
    /// <summary>Cultures: flags drawn from stripes, picked once per game, prefab swaps, snapshots and the shipped data.</summary>
    public sealed class CultureTests
    {
        private static readonly Color Blue = new Color(0.05f, 0.37f, 0.69f);
        private readonly List<Object> _made = new List<Object>();
        private readonly List<CultureChosen> _chosen = new List<CultureChosen>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _chosen.Clear();
            EventBus.Subscribe<CultureChosen>(_chosen.Add);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            foreach (Object made in _made) Object.DestroyImmediate(made);
            _made.Clear();
        }

        [Test]
        public void VerticalTricolour_IsDrawnLeftToRight()
        {
            var flag = new FlagDesign { vertical = true, stripes = new[] { Color.green, Color.white, Color.red } };
            Color32[] pixels = FlagPainter.Paint(flag, 30, 2);
            Assert.AreEqual((Color32)Color.green, pixels[0]);
            Assert.AreEqual((Color32)Color.white, pixels[15]);
            Assert.AreEqual((Color32)Color.red, pixels[29]);
            Assert.AreEqual((Color32)Color.red, pixels[30 + 29], "every row is the same");
        }

        [Test]
        public void StripeWeights_MakeTheMiddleStripeTaller()
        {
            var flag = new FlagDesign { stripes = new[] { Color.red, Color.yellow, Color.red }, stripeWeights = new[] { 1f, 2f, 1f } };
            Color32[] pixels = FlagPainter.Paint(flag, 1, 4); // rows run bottom to top
            Assert.AreEqual((Color32)Color.red, pixels[3], "top");
            Assert.AreEqual((Color32)Color.yellow, pixels[2]);
            Assert.AreEqual((Color32)Color.yellow, pixels[1]);
            Assert.AreEqual((Color32)Color.red, pixels[0], "bottom");
        }

        [Test]
        public void Canton_HasACrossInTheTopLeftCorner()
        {
            var flag = new FlagDesign
            {
                stripes = new[] { Blue, Color.white, Blue, Color.white, Blue, Color.white, Blue, Color.white, Blue },
                canton = true, cantonSize = new Vector2(10f / 27f, 10f / 18f), cantonColor = Blue, crossColor = Color.white, crossWidth = 0.2f,
            };
            Color32[] pixels = FlagPainter.Paint(flag, 27, 18);
            Color32 At(int x, int fromTop) => pixels[(17 - fromTop) * 27 + x];

            Assert.AreEqual((Color32)Blue, At(0, 0), "canton corner");
            Assert.AreEqual((Color32)Color.white, At(5, 0), "upright bar of the cross");
            Assert.AreEqual((Color32)Color.white, At(0, 5), "crossbar");
            Assert.AreEqual((Color32)Blue, At(20, 0), "first stripe beside the canton");
            Assert.AreEqual((Color32)Color.white, At(20, 2), "second stripe");
            Assert.AreEqual((Color32)Blue, At(0, 17), "last stripe runs under the canton");
        }

        [Test]
        public void Culture_IsChosenOnce_ForAVillageFromBeforeCultures()
        {
            var meta = new MetaModel { villageName = "Old Bay" };
            var analytics = new MockAnalyticsService(false);
            var village = new VillageService(Made(ScriptableObject.CreateInstance<VillageConfig>()), meta, analytics, id => id == "greece" || id == "italy");
            Assert.IsTrue(village.NeedsCulture);

            Assert.IsFalse(village.TryChooseCulture("atlantis", out string error));
            Assert.AreEqual("Pick a culture.", error);
            Assert.IsTrue(village.TryChooseCulture("italy", out error));
            Assert.AreEqual("italy", village.CultureId);
            Assert.IsFalse(village.NeedsCulture);
            Assert.IsTrue(analytics.HasLogged(AnalyticsEvents.CultureChosen));
            Assert.AreEqual(1, _chosen.Count);
            Assert.AreEqual("italy", _chosen[0].CultureId);

            Assert.IsFalse(village.TryChooseCulture("greece", out error));
            Assert.AreEqual("Your culture is already chosen.", error);
            Assert.AreEqual("Old Bay", village.Name, "the name is kept");
        }

        [Test]
        public void Founding_NeedsAKnownCulture()
        {
            var meta = new MetaModel();
            var village = new VillageService(Made(ScriptableObject.CreateInstance<VillageConfig>()), meta, new MockAnalyticsService(false), id => id == "spain");
            Assert.IsFalse(village.TryChooseCulture("spain", out string error));
            Assert.AreEqual("Found the village first.", error);
            Assert.IsFalse(village.TryFound("Sol Bay", string.Empty, out error));
            Assert.AreEqual("Pick a culture.", error);
            Assert.IsFalse(village.TryFound("Sol Bay", "france", out _));
            Assert.IsFalse(village.IsFounded, "nothing is set until both are good");

            Assert.IsTrue(village.TryFound("Sol Bay", "spain", out _));
            Assert.AreEqual("spain", meta.cultureId);
            Assert.AreEqual(1, _chosen.Count);
        }

        [Test]
        public void SuggestName_PrefersTheCulturesNames()
        {
            var village = new VillageService(Made(ScriptableObject.CreateInstance<VillageConfig>()), new MetaModel(), new MockAnalyticsService(false), seed: 3);
            string[] greek = { "Kalamaki", "Paralia" };
            for (int i = 0; i < 10; i++) CollectionAssert.Contains(greek, village.SuggestName(greek));
        }

        [Test]
        public void Resolve_UsesTheCulturesVersion_OrFallsBack()
        {
            GameObject tent = Made(new GameObject("Tent"));
            GameObject greekTent = Made(new GameObject("GreekTent"));
            GameObject cabin = Made(new GameObject("Cabin"));
            CultureDefinition culture = Culture("greece");
            culture.prefabSwaps = new[] { new PrefabSwap { original = tent, replacement = greekTent }, new PrefabSwap { original = cabin } };

            Assert.AreSame(greekTent, culture.Resolve(tent));
            Assert.AreSame(cabin, culture.Resolve(cabin), "an empty slot keeps today's object");
            Assert.IsNull(culture.Resolve(null));
        }

        [Test]
        public void Service_FindsCultures_AndKeepsOneFlagImageEach()
        {
            var database = Made(ScriptableObject.CreateInstance<GameDatabase>());
            database.village = Made(ScriptableObject.CreateInstance<VillageConfig>());
            database.cultures = new[] { Culture("greece"), Culture("spain") };
            var meta = new MetaModel();
            var service = new CultureService(database, meta);

            Assert.IsNull(service.Current);
            Assert.IsFalse(service.IsKnown("italy"));
            meta.cultureId = "spain";
            Assert.AreSame(database.cultures[1], service.Current);

            Texture2D flag = service.Flag(service.Current);
            Assert.AreEqual(database.village.flagPixels, new Vector2Int(flag.width, flag.height));
            Assert.AreSame(flag, service.Flag(service.Current), "made once");
            Assert.IsNull(service.Flag(null));
            service.Dispose();
        }

        [Test]
        public void Snapshots_DropUnknownCultures()
        {
            using (var data = new OnlineTestData())
            {
                data.Database.cultures = new[] { Culture("italy") };
                Assert.AreEqual("italy", IslandSnapshotValidator.Sanitize(new IslandSnapshot { ownerId = "a", cultureId = "italy" }, data.Database).cultureId);
                Assert.AreEqual(string.Empty, IslandSnapshotValidator.Sanitize(new IslandSnapshot { ownerId = "a", cultureId = "<b>mars" }, data.Database).cultureId);
                Assert.AreEqual(string.Empty, IslandSnapshotValidator.Sanitize(new IslandSnapshot { ownerId = "a" }, data.Database).cultureId,
                    "snapshots from before cultures have none");
            }
        }

        [Test]
        public void ShippedCultures_AreGreeceSpainItaly_WithFlagsAndNames()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));

            CollectionAssert.AreEqual(new[] { "greece", "spain", "italy" }, System.Array.ConvertAll(database.cultures, c => c.id));
            Assert.IsNotNull(database.village.flagpolePrefab.GetComponent<FlagView>(), "the flagpole prefab shows the flag");
            foreach (CultureDefinition culture in database.cultures)
            {
                Assert.IsNotEmpty(culture.displayName, culture.id);
                Assert.GreaterOrEqual(culture.flag.stripes.Length, 3, culture.id);
                Assert.GreaterOrEqual(culture.villageNames.Length, 5, culture.id);
                foreach (string name in culture.villageNames)
                {
                    Assert.IsNull(VillageNames.Check(name, database.village.minNameLength, database.village.maxNameLength, out _), $"{culture.id}: '{name}'");
                }

                foreach (PaletteRecolor recolor in culture.recolors)
                {
                    Assert.IsNotNull(database.palette.GetMaterial(recolor.paletteKey), $"{culture.id} recolours '{recolor.paletteKey}'");
                }
            }
        }

        private CultureDefinition Culture(string id)
        {
            var culture = Made(ScriptableObject.CreateInstance<CultureDefinition>());
            culture.id = id;
            culture.displayName = id;
            return culture;
        }

        private T Made<T>(T made) where T : Object
        {
            _made.Add(made);
            return made;
        }
    }
}
