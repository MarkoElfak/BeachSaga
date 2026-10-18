using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Village names: tidying and checks, founding once with a culture that never changes, renaming, saves.</summary>
    public sealed class VillageTests
    {
        private readonly List<VillageRenamed> _renames = new List<VillageRenamed>();
        private VillageConfig _config;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _renames.Clear();
            EventBus.Subscribe<VillageRenamed>(_renames.Add);
            _config = ScriptableObject.CreateInstance<VillageConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            Object.DestroyImmediate(_config);
        }

        [TestCase("  Sunny    Bay ", "Sunny Bay")]
        [TestCase("Agia Thalassa", "Agia Thalassa")]
        [TestCase("Ševa's Cove", "Ševa's Cove")]
        [TestCase("Bay & Co.", "Bay & Co.")]
        [TestCase("Port-2", "Port-2")]
        public void Names_AreTidiedAndAccepted(string raw, string expected)
        {
            Assert.IsNull(VillageNames.Check(raw, 2, 20, out string clean));
            Assert.AreEqual(expected, clean);
        }

        [TestCase("", "Give your village a name.")]
        [TestCase("   ", "Give your village a name.")]
        [TestCase(null, "Give your village a name.")]
        [TestCase("A", "Use at least 2 characters.")]
        [TestCase("A very very long village name", "Use at most 20 characters.")]
        [TestCase("Bay <b>", "Use letters, numbers, spaces and - ' . & only.")]
        [TestCase("Sun\nBay!", "Use letters, numbers, spaces and - ' . & only.")]
        public void Names_ThatDoNotFit_GetAMessage(string raw, string message)
        {
            Assert.AreEqual(message, VillageNames.Check(raw, 2, 20, out _));
        }

        [Test]
        public void Found_SetsNameAndCultureOnce_AndLogsIt()
        {
            var meta = new MetaModel();
            var analytics = new MockAnalyticsService(false);
            var village = new VillageService(_config, meta, analytics, seed: 1);
            Assert.IsFalse(village.IsFounded);

            Assert.IsFalse(village.TryFound("A", "greece", out string error));
            Assert.AreEqual("Use at least 2 characters.", error);
            Assert.IsFalse(village.IsFounded);
            Assert.AreEqual(string.Empty, meta.cultureId, "a refused name sets nothing");

            Assert.IsTrue(village.TryFound(" Sunny  Bay", "greece", out error));
            Assert.IsNull(error);
            Assert.AreEqual("Sunny Bay", meta.villageName);
            Assert.AreEqual("greece", village.CultureId);
            Assert.IsTrue(analytics.HasLogged(AnalyticsEvents.VillageFounded));
            Assert.AreEqual(1, _renames.Count);
            Assert.AreEqual("Sunny Bay", _renames[0].Name);

            Assert.IsTrue(village.TryFound("Other Bay", "spain", out _));
            Assert.AreEqual("greece", village.CultureId, "the culture is picked once for the whole game");
        }

        [Test]
        public void Rename_ChangesOnlyTheName()
        {
            var meta = new MetaModel();
            var village = new VillageService(_config, meta, new MockAnalyticsService(false), seed: 1);
            Assert.IsFalse(village.TryRename("Coral Cove", out string error), "nothing to rename before founding");
            Assert.AreEqual("Found the village first.", error);

            village.TryFound("Sunny Bay", "italy", out _);
            Assert.IsTrue(village.TryRename("Coral Cove", out _));
            Assert.AreEqual("Coral Cove", village.Name);
            Assert.AreEqual("italy", village.CultureId);
            Assert.IsFalse(village.TryRename("<i>", out _));
            Assert.AreEqual("Coral Cove", village.Name);

            Assert.IsTrue(village.TryRename("Coral  Cove", out _));
            Assert.AreEqual(2, _renames.Count, "the same name again is not announced");
        }

        [Test]
        public void SuggestName_ComesFromTheConfig_AndNeverRepeatsInARow()
        {
            var village = new VillageService(_config, new MetaModel(), new MockAnalyticsService(false), seed: 7);
            string previous = null;
            for (int i = 0; i < 30; i++)
            {
                string name = village.SuggestName();
                CollectionAssert.Contains(_config.suggestedNames, name);
                Assert.IsNull(VillageNames.Check(name, _config.minNameLength, _config.maxNameLength, out _), $"'{name}' is a valid name");
                Assert.AreNotEqual(previous, name);
                previous = name;
            }
        }

        [Test]
        public void Version14Save_IsMigrated_AndAsksForAVillage()
        {
            string json = "{\"version\":14,\"wallet\":{\"coins\":42},\"meta\":{\"tutorialStep\":9}}";
            string migrated = new SaveMigrator(SaveMigrations.All).MigrateToCurrent(json, 14);
            var data = JsonUtility.FromJson<GameSaveData>(migrated);

            Assert.AreEqual(GameSaveData.CurrentVersion, data.version);
            Assert.AreEqual(42, data.wallet.coins, "progress is kept");
            Assert.AreEqual(9, data.meta.tutorialStep);
            Assert.AreEqual(string.Empty, data.meta.villageName);
            Assert.AreEqual(string.Empty, data.meta.cultureId);
            Assert.IsFalse(new VillageService(_config, data.meta, new MockAnalyticsService(false)).IsFounded,
                "the start screen asks for a name once");
        }

        [Test]
        public void Snapshot_KeepsTheVillageName_WithoutAngleBrackets()
        {
            using (var data = new OnlineTestData())
            {
                var raw = new IslandSnapshot { ownerId = "x", ownerName = "Kai", villageName = "Palm <b>Harbour</b>" };
                IslandSnapshot clean = IslandSnapshotValidator.Sanitize(raw, data.Database);
                Assert.AreEqual("Palm bHarbour/b", clean.villageName);
                Assert.AreEqual(string.Empty, IslandSnapshotValidator.Sanitize(new IslandSnapshot { ownerId = "y" }, data.Database).villageName,
                    "snapshots from before villages have no name");
            }
        }
    }
}
