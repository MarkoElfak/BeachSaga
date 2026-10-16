using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Settings volumes and their save migration, and which moments shake the camera or throw confetti.</summary>
    public sealed class FeelTests
    {
        private PresentationConfig _config;
        private FeelDirector _feel;
        private int _shakes, _confetti, _settingsChanged;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _config = ScriptableObject.CreateInstance<PresentationConfig>();
            _feel = new FeelDirector(_config);
            _shakes = _confetti = _settingsChanged = 0;
            EventBus.Subscribe<CameraShakeRequested>(evt => _shakes++);
            EventBus.Subscribe<ConfettiRequested>(evt => _confetti += evt.Pieces);
            EventBus.Subscribe<SettingsChanged>(evt => _settingsChanged++);
        }

        [TearDown]
        public void TearDown()
        {
            _feel.Dispose();
            Object.DestroyImmediate(_config);
            EventBus.ClearAll();
        }

        [Test]
        public void AFinishedBuildingShakesAndThrowsConfetti()
        {
            EventBus.Publish(new TierUpgraded(0, 2));
            Assert.AreEqual(1, _shakes);
            Assert.AreEqual(_config.tierConfetti, _confetti);
        }

        [Test]
        public void LevelsTrophiesAndNeighboursThrowConfetti()
        {
            EventBus.Publish(new MiniGameLevelPassed("DonutRush", "donuts", 1));
            EventBus.Publish(new AchievementCompleted(0));
            EventBus.Publish(new NeighbourMovedIn(0, 1));
            Assert.AreEqual(_config.levelConfetti + _config.trophyConfetti + _config.neighbourConfetti, _confetti);
            Assert.AreEqual(0, _shakes);
        }

        [Test]
        public void VolumesClampAndAnnounceChanges()
        {
            var settings = new SettingsService(new SettingsModel());
            settings.SoundVolume = 1.5f;
            Assert.AreEqual(0, _settingsChanged, "already at full volume");
            settings.SoundVolume = 0.4f;
            settings.MusicVolume = -1f;
            Assert.AreEqual(0.4f, settings.SoundVolume, 1e-5f);
            Assert.AreEqual(0f, settings.MusicVolume);
            Assert.IsFalse(settings.MusicOn);
            Assert.AreEqual(2, _settingsChanged);
        }

        [Test]
        public void OldSoundSwitchesBecomeVolumes()
        {
            const string v12 = "{\"version\":12,\"meta\":{\"settings\":{\"soundOn\":false,\"musicOn\":true}}}";
            string json = new SaveMigrator(SaveMigrations.All).MigrateToCurrent(v12, 12);
            var data = JsonUtility.FromJson<GameSaveData>(json);
            Assert.AreEqual(GameSaveData.CurrentVersion, data.version);
            Assert.AreEqual(0f, data.meta.settings.soundVolume, "sound was off");
            Assert.AreEqual(1f, data.meta.settings.musicVolume, "music was on");
            Assert.IsTrue(data.meta.settings.vibrationOn);
        }
    }
}
