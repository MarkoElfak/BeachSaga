using BeachTycoon.Data;
using BeachTycoon.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Release (M22): real ads need both LevelPlay keys, and a Google Play bundle is refused while the upload key,
    /// its passwords, the privacy policy or the ads keys are missing.
    /// </summary>
    public sealed class StoreReadinessTests
    {
        private MonetizationConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<MonetizationConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void RealAds_NeedTheAppKeyAndTheRewardedAdUnit()
        {
            // Both platforms get the same keys, so the test holds whichever build target the Editor has.
            Assert.IsFalse(_config.HasAdsKeys, "a new config keeps the mock ads");
            _config.androidAppKey = _config.iosAppKey = "1f2e3d4c";
            Assert.IsFalse(_config.HasAdsKeys, "the app key alone is not enough");
            _config.androidRewardedAdUnit = _config.iosRewardedAdUnit = "  ";
            Assert.IsFalse(_config.HasAdsKeys, "blank counts as missing");
            _config.androidRewardedAdUnit = _config.iosRewardedAdUnit = "abcd1234";
            Assert.IsTrue(_config.HasAdsKeys);
        }

        [Test]
        public void ANewProject_IsMissingEverything()
        {
            var problems = StoreReadiness.Problems(_config, false, false, false);
            Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
            StringAssert.Contains("upload key", problems[0]);
            StringAssert.Contains("privacy", problems[1]);
            StringAssert.Contains("LevelPlay", problems[2]);
        }

        [Test]
        public void AKeyWithoutPasswords_AsksForThePasswords()
        {
            Ready();
            var problems = StoreReadiness.Problems(_config, true, true, false);
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("passwords", problems[0]);
        }

        [Test]
        public void EverythingSet_IsReady()
        {
            Ready();
            Assert.IsEmpty(StoreReadiness.Problems(_config, true, true, true));
        }

        private void Ready()
        {
            _config.privacyPolicyUrl = "https://example.com/privacy";
            _config.androidAppKey = "1f2e3d4c";
            _config.androidRewardedAdUnit = "abcd1234";
        }
    }
}
