using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Day and night music: the crossfade follows the clock and the volume follows the setting.</summary>
    public sealed class MusicPlayerTests
    {
        private SoundLibrary _library;
        private AudioClip _day, _night;
        private DayNightConfig _config;
        private MetaModel _meta;
        private DayClock _clock;
        private SettingsService _settings;
        private GameObject _host;
        private MusicPlayer _music;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _day = AudioClip.Create("day", 64, 1, 22050, false);
            _night = AudioClip.Create("night", 64, 1, 22050, false);
            _library = ScriptableObject.CreateInstance<SoundLibrary>();
            _library.music = _day;
            _library.nightMusic = _night;
            _library.musicVolume = 0.5f;
            _library.musicFadeSeconds = 4f;
            _config = ScriptableObject.CreateInstance<DayNightConfig>();
            _meta = new MetaModel { dayPhase = 0.25f };
            _clock = new DayClock(_config, _meta);
            _settings = new SettingsService(new SettingsModel());
            _host = new GameObject("Music");
            _music = new MusicPlayer(_library, _settings, _clock, _host);
        }

        [TearDown]
        public void TearDown()
        {
            _music.Dispose();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_day);
            Object.DestroyImmediate(_night);
            EventBus.ClearAll();
        }

        [Test]
        public void DaytimePlaysTheDayLoop()
        {
            Assert.AreEqual(0f, _music.NightMix);
            Assert.AreEqual(0.5f, _music.DayVolume, 1e-4f);
            Assert.AreEqual(0f, _music.NightVolume, 1e-4f);
        }

        [Test]
        public void NightfallCrossfadesOverTheFadeTime()
        {
            _clock.SetPhase(0.75f);
            _music.Tick(2f);
            Assert.AreEqual(0.5f, _music.NightMix, 1e-4f);
            Assert.AreEqual(_music.DayVolume, _music.NightVolume, 1e-4f, "halfway: both loops equally loud");
            Assert.AreEqual(0.5f * Mathf.Sqrt(0.5f), _music.DayVolume, 1e-4f, "equal-power crossfade");
            _music.Tick(5f);
            Assert.AreEqual(1f, _music.NightMix);
            Assert.AreEqual(0.5f, _music.NightVolume, 1e-4f);
            Assert.AreEqual(0f, _music.DayVolume, 1e-4f);
        }

        [Test]
        public void MusicVolumeSettingScalesAndMutes()
        {
            _settings.MusicVolume = 0.5f;
            Assert.AreEqual(0.25f, _music.DayVolume, 1e-4f);
            _settings.MusicVolume = 0f;
            Assert.AreEqual(0f, _music.DayVolume, 1e-4f);
        }
    }
}
