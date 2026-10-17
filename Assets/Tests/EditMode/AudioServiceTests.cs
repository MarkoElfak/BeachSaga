using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Sound and vibration hooks: volume, repeat limit, vibration setting and the new event hooks.</summary>
    public sealed class AudioServiceTests
    {
        private SoundLibrary _library;
        private AudioClip _clip;
        private GameObject _host;
        private SettingsService _settings;
        private FakeHaptics _haptics;
        private AudioService _audio;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _clip = AudioClip.Create("beep", 64, 1, 22050, false);
            _library = ScriptableObject.CreateInstance<SoundLibrary>();
            _library.sounds = new[]
            {
                new SoundEntry { id = SoundId.Sale, clip = _clip, volume = 0.3f, minInterval = 0.12f },
                new SoundEntry { id = SoundId.StandUpgrade, clip = _clip, volume = 0.8f, haptic = HapticStrength.Medium },
                new SoundEntry { id = SoundId.Trophy, clip = _clip, volume = 0.8f, haptic = HapticStrength.Heavy },
                new SoundEntry { id = SoundId.Place, clip = _clip, volume = 0.8f },
                new SoundEntry { id = SoundId.UiClick, volume = 1f, haptic = HapticStrength.Light }, // no clip yet
            };
            _host = new GameObject("Audio");
            _settings = new SettingsService(new SettingsModel());
            _haptics = new FakeHaptics();
            _audio = new AudioService(_library, _settings, _host, _haptics);
        }

        [TearDown]
        public void TearDown()
        {
            _audio.Dispose();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_clip);
            EventBus.ClearAll();
        }

        [Test]
        public void PlaysTheClipAndVibrates()
        {
            _audio.Play(SoundId.StandUpgrade, 0f);
            Assert.AreEqual(1, _audio.TimesPlayed(SoundId.StandUpgrade));
            Assert.AreEqual(1, _haptics.PulseCount);
            Assert.AreEqual(HapticStrength.Medium, _haptics.LastStrength);
        }

        [Test]
        public void BusySoundsAreRateLimited()
        {
            _audio.Play(SoundId.Sale, 0f);
            _audio.Play(SoundId.Sale, 0.05f);
            _audio.Play(SoundId.Sale, 0.2f);
            Assert.AreEqual(2, _audio.TimesPlayed(SoundId.Sale));
            Assert.AreEqual(3, _audio.HooksFired);
        }

        [Test]
        public void SoundVolumeZeroIsSilentButStillVibrates()
        {
            _settings.SoundVolume = 0f;
            _audio.Play(SoundId.StandUpgrade, 0f);
            Assert.AreEqual(0, _audio.SoundsPlayed);
            Assert.AreEqual(1, _haptics.PulseCount);
        }

        [Test]
        public void VibrationOffMeansNoPulses()
        {
            _settings.VibrationOn = false;
            _audio.Play(SoundId.StandUpgrade, 0f);
            Assert.AreEqual(1, _audio.SoundsPlayed);
            Assert.AreEqual(0, _haptics.PulseCount);
        }

        [Test]
        public void ASlotWithoutAClipStillVibrates()
        {
            _audio.Play(SoundId.UiClick, 0f);
            Assert.AreEqual(0, _audio.SoundsPlayed);
            Assert.AreEqual(HapticStrength.Light, _haptics.LastStrength);
        }

        [Test]
        public void NewEventsHaveHooks()
        {
            EventBus.Publish(new AchievementCompleted(0));
            Assert.AreEqual(SoundId.Trophy, _audio.LastPlayed);
            EventBus.Publish(new MoveEnded(false));
            Assert.AreEqual(0, _audio.TimesPlayed(SoundId.Place), "a cancelled move is silent");
            EventBus.Publish(new MoveEnded(true));
            Assert.AreEqual(1, _audio.TimesPlayed(SoundId.Place));
        }

        private sealed class FakeHaptics : IHapticsService
        {
            public int PulseCount { get; private set; }
            public HapticStrength LastStrength { get; private set; }

            public void Play(HapticStrength strength)
            {
                if (strength == HapticStrength.None) return;
                PulseCount++;
                LastStrength = strength;
            }
        }
    }
}
