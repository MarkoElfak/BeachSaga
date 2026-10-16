using System;
using System.IO;
using System.Text.RegularExpressions;
using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachTycoon.Tests
{
    public sealed class SaveServiceTests
    {
        private const float AutosaveSeconds = 5f;
        private const long StartingCoins = 50;

        private sealed class FixedClock : ITimeProvider
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        }

        private string _directory;
        private FixedClock _clock;

        private string SavePath => Path.Combine(_directory, SaveService.FileName);

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "BeachTycoonTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _clock = new FixedClock();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private SaveService Create() => new SaveService(_directory, AutosaveSeconds, StartingCoins, _clock);

        [Test]
        public void NoFile_StartsNewGame()
        {
            SaveService save = Create();
            Assert.IsFalse(save.LoadedFromDisk);
            Assert.AreEqual(StartingCoins, save.Data.wallet.coins);
            Assert.IsNull(save.PreviousSessionUtc);
            Assert.IsFalse(File.Exists(SavePath), "loading must not write");
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryModel()
        {
            SaveService first = Create();
            first.Data.wallet.coins = 1234;
            first.Data.lots.Add(new LotModel(0) { state = LotState.Built, tier = 2, placed = true, x = 1.5f, z = -3f });
            first.Data.stands.Add(new StandModel { productId = "donut", level = 3 });
            first.Data.decorations.Add(new DecorationModel { decorationId = "palm", spotIndex = 2, placed = true, x = 4f, z = 2.5f });
            first.Data.meta.tutorialStep = 5;
            first.Data.meta.settings.musicOn = false;
            first.Save();

            SaveService second = Create();
            GameSaveData data = second.Data;
            Assert.IsTrue(second.LoadedFromDisk);
            Assert.AreEqual(1234, data.wallet.coins);
            Assert.AreEqual(LotState.Built, data.lots[0].state);
            Assert.AreEqual(2, data.lots[0].tier);
            Assert.IsTrue(data.lots[0].placed);
            Assert.AreEqual(new Vector2(1.5f, -3f), new Vector2(data.lots[0].x, data.lots[0].z));
            Assert.AreEqual("donut", data.stands[0].productId);
            Assert.AreEqual(3, data.stands[0].level);
            Assert.AreEqual("palm", data.decorations[0].decorationId);
            Assert.AreEqual(4f, data.decorations[0].x);
            Assert.AreEqual(5, data.meta.tutorialStep);
            Assert.IsFalse(data.meta.settings.musicOn);
            Assert.AreEqual(_clock.UtcNow, second.PreviousSessionUtc);
        }

        [Test]
        public void Save_WritesCurrentVersionAndLeavesNoTempFile()
        {
            SaveService save = Create();
            save.Data.version = 1;
            save.Save();

            var header = JsonUtility.FromJson<SaveVersionHeader>(File.ReadAllText(SavePath));
            Assert.AreEqual(GameSaveData.CurrentVersion, header.version);
            Assert.AreEqual(1, Directory.GetFiles(_directory).Length, "only the save file remains");
        }

        [Test]
        public void Autosave_WritesAfterInterval()
        {
            SaveService save = Create();
            save.Tick(AutosaveSeconds * 0.5f);
            Assert.IsFalse(File.Exists(SavePath));
            save.Tick(AutosaveSeconds * 0.6f);
            Assert.IsTrue(File.Exists(SavePath));
        }

        [Test]
        public void CorruptFile_StartsNewGameAndKeepsACopy()
        {
            File.WriteAllText(SavePath, "{ this is not json");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save"));

            SaveService save = Create();

            Assert.IsFalse(save.LoadedFromDisk);
            Assert.AreEqual(StartingCoins, save.Data.wallet.coins);
            Assert.IsTrue(File.Exists(SavePath + ".corrupt"));
        }

        [Test]
        public void EmptyObject_IsTreatedAsCorrupt()
        {
            File.WriteAllText(SavePath, "{}");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save"));
            Assert.IsFalse(Create().LoadedFromDisk);
        }

        [Test]
        public void NewerVersion_StartsNewGameInsteadOfCrashing()
        {
            File.WriteAllText(SavePath, "{\"version\":99,\"wallet\":{\"coins\":5}}");
            LogAssert.Expect(LogType.Warning, new Regex("newer than this build"));
            Assert.IsFalse(Create().LoadedFromDisk);
        }

        [Test]
        public void Version1File_IsMigratedAndGetsDefaultsForNewData()
        {
            File.WriteAllText(SavePath,
                "{\"version\":1,\"lastSeenUtc\":\"2026-06-30T10:00:00.0000000Z\",\"wallet\":{\"coins\":42}," +
                "\"lots\":[{\"index\":0,\"state\":2,\"tier\":1}],\"stands\":[]}");

            SaveService save = Create();

            Assert.IsTrue(save.LoadedFromDisk);
            Assert.AreEqual(GameSaveData.CurrentVersion, save.Data.version);
            Assert.AreEqual(42, save.Data.wallet.coins);
            Assert.IsNotNull(save.Data.neighbours);
            Assert.IsNotNull(save.Data.decorations);
            Assert.IsNotNull(save.Data.meta);
            Assert.IsNotNull(save.Data.meta.settings);
            Assert.AreEqual(0, save.Data.meta.dailyStreak, "old saves have never claimed a daily reward");
            Assert.AreEqual(string.Empty, save.Data.meta.dailyLastClaimDay);
            Assert.IsNotNull(save.Data.meta.visitBonusOwners, "old saves have no visit bonus record");
            Assert.IsEmpty(save.Data.meta.visitBonusOwners);
            Assert.IsFalse(save.Data.lots[0].placed, "old lots keep their layout position");
            Assert.AreEqual(new DateTime(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc), save.PreviousSessionUtc);
        }

        [Test]
        public void Migrator_HasAStepForEveryOldVersion()
        {
            var migrator = new SaveMigrator(SaveMigrations.All);
            for (int version = 1; version < GameSaveData.CurrentVersion; version++)
            {
                string json = migrator.MigrateToCurrent($"{{\"version\":{version}}}", version);
                Assert.AreEqual(GameSaveData.CurrentVersion, JsonUtility.FromJson<SaveVersionHeader>(json).version,
                    $"from v{version}");
            }
        }

        [Test]
        public void Migrator_ThrowsWhenAStepIsMissing()
        {
            var migrator = new SaveMigrator();
            Assert.Throws<InvalidOperationException>(() => migrator.MigrateToCurrent("{\"version\":1}", 1));
        }
    }
}
