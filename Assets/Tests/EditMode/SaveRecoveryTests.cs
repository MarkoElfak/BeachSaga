using System;
using System.IO;
using System.Text.RegularExpressions;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachTycoon.Tests
{
    /// <summary>Backup copies and recovery from damaged save files.</summary>
    public sealed class SaveRecoveryTests
    {
        private sealed class FixedClock : ITimeProvider
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        }

        private string _directory;

        private string SavePath => Path.Combine(_directory, SaveService.FileName);

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "BeachTycoonTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private SaveService Create() => new SaveService(_directory, 5f, 50, new FixedClock());

        // Two saves with different coins: the file holds the second, the backup the first.
        private void SaveTwice(long first, long second)
        {
            SaveService save = Create();
            save.Data.wallet.coins = first;
            save.Save();
            save.Data.wallet.coins = second;
            save.Save();
        }

        [Test]
        public void SecondSave_KeepsThePreviousOneAsBackup()
        {
            SaveService save = Create();
            save.Save();
            Assert.IsFalse(File.Exists(save.BackupPath), "the first save has nothing to back up");

            SaveTwice(100, 200);
            Assert.IsTrue(File.Exists(SavePath + ".bak"));
            Assert.AreEqual(SaveRecovery.None, Create().Recovery);
            Assert.AreEqual(200, Create().Data.wallet.coins);
        }

        [Test]
        public void DamagedSave_IsRestoredFromBackup()
        {
            SaveTwice(100, 200);
            File.WriteAllText(SavePath, "{ broken");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save"));
            LogAssert.Expect(LogType.Warning, new Regex("Save restored"));

            SaveService save = Create();

            Assert.IsTrue(save.LoadedFromDisk);
            Assert.AreEqual(SaveRecovery.FromBackup, save.Recovery);
            Assert.AreEqual(100, save.Data.wallet.coins);
            Assert.IsTrue(File.Exists(SavePath + ".corrupt"), "the broken file is kept");
        }

        [Test]
        public void SavingAfterRecovery_DoesNotOverwriteTheGoodBackupWithTheBrokenFile()
        {
            SaveTwice(100, 200);
            File.WriteAllText(SavePath, "{ broken");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save"));
            LogAssert.Expect(LogType.Warning, new Regex("Save restored"));
            SaveService save = Create();
            save.Data.wallet.coins = 150;
            save.Save();

            Assert.AreEqual(100, JsonUtility.FromJson<Data.GameSaveData>(File.ReadAllText(save.BackupPath)).wallet.coins);
            Assert.AreEqual(150, Create().Data.wallet.coins);
        }

        [Test]
        public void UnfinishedWrite_IsUsedWhenTheSaveIsDamaged()
        {
            SaveTwice(100, 200);
            File.Copy(SavePath, SavePath + ".tmp");
            File.WriteAllText(SavePath, string.Empty);
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save"));
            LogAssert.Expect(LogType.Warning, new Regex("Save restored"));

            SaveService save = Create();

            Assert.AreEqual(SaveRecovery.FromUnfinishedWrite, save.Recovery);
            Assert.AreEqual(200, save.Data.wallet.coins);
        }

        [Test]
        public void NothingReadable_StartsNewGameAndSaysSo()
        {
            SaveTwice(100, 200);
            File.WriteAllText(SavePath, "{ broken");
            File.WriteAllText(SavePath + ".bak", "{}");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save beach_save.json "));
            LogAssert.Expect(LogType.Warning, new Regex("Could not load save beach_save.json.bak"));
            LogAssert.Expect(LogType.Warning, new Regex("starting a new game"));

            SaveService save = Create();

            Assert.IsFalse(save.LoadedFromDisk);
            Assert.AreEqual(SaveRecovery.NewGame, save.Recovery);
            Assert.AreEqual(50, save.Data.wallet.coins);
        }

        [Test]
        public void NoFilesAtAll_IsANormalNewGame()
        {
            Assert.AreEqual(SaveRecovery.None, Create().Recovery);
        }
    }
}
