using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    public sealed class DayClockTests
    {
        private DayNightConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DayNightConfig>();
            _config.cycleMinutes = 10f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Phase_AdvancesWithPlayTime_AndIsSaved()
        {
            var meta = new MetaModel { dayPhase = 0f };
            var clock = new DayClock(_config, meta);
            clock.Tick(150f);
            Assert.AreEqual(0.25f, clock.Phase, 1e-4f, "a quarter of a 10 minute cycle");
            Assert.AreEqual(clock.Phase, meta.dayPhase);
        }

        [Test]
        public void Phase_WrapsAroundAndIgnoresNegativeTime()
        {
            var clock = new DayClock(_config, new MetaModel { dayPhase = 0.9f });
            clock.Advance(120f);
            Assert.AreEqual(0.1f, clock.Phase, 1e-4f);
            clock.Advance(-50f);
            Assert.AreEqual(0.1f, clock.Phase, 1e-4f);
            Assert.AreEqual(0.5f, new DayClock(_config, new MetaModel { dayPhase = 2.5f }).Phase, 1e-4f);
        }

        [Test]
        public void Night_IsBetweenSunsetAndDawn()
        {
            var clock = new DayClock(_config, new MetaModel());
            clock.SetPhase(0.25f);
            Assert.IsFalse(clock.IsNight);
            clock.SetPhase(0.75f);
            Assert.IsTrue(clock.IsNight);
        }

        [Test]
        public void Defaults_MakeNightDarkerAndBluerThanMidday()
        {
            Color noon = _config.sunColor.Evaluate(0.25f);
            Color night = _config.sunColor.Evaluate(0.75f);
            Assert.Less(_config.sunIntensity.Evaluate(0.75f), _config.sunIntensity.Evaluate(0.25f));
            Assert.Greater(night.b, night.r, "moonlight is blue");
            Assert.Greater(noon.r, night.r);
        }
    }
}
