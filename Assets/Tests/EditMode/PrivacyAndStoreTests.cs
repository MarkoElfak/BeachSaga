using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    /// <summary>The privacy answer, the Remove ads gate on rewarded ads, and the mock store.</summary>
    public sealed class PrivacyAndStoreTests
    {
        private const string RemoveAds = "remove_ads";

        private readonly List<PrivacyChanged> _changes = new List<PrivacyChanged>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _changes.Clear();
            EventBus.Subscribe<PrivacyChanged>(_changes.Add);
        }

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        [Test]
        public void Privacy_StartsUnansweredWithNothingAllowed()
        {
            var privacy = new PrivacyService(new SettingsModel());
            Assert.IsFalse(privacy.HasAnswered);
            Assert.IsFalse(privacy.AnalyticsAllowed);
            Assert.IsFalse(privacy.PersonalizedAds);
        }

        [Test]
        public void Privacy_AcceptAndDecline_AreSavedAndAnnounced()
        {
            var settings = new SettingsModel();
            var privacy = new PrivacyService(settings);

            privacy.Answer(PrivacyChoice.Accepted);
            Assert.AreEqual((int)PrivacyChoice.Accepted, settings.privacy);
            Assert.IsTrue(privacy.AnalyticsAllowed && privacy.PersonalizedAds);
            privacy.Answer(PrivacyChoice.Declined);
            Assert.IsFalse(privacy.AnalyticsAllowed);

            Assert.AreEqual(2, _changes.Count);
            Assert.IsTrue(_changes[0].AnalyticsAllowed);
            Assert.IsFalse(_changes[1].AnalyticsAllowed);
        }

        [Test]
        public void Privacy_SameAnswerOrNotAsked_ChangesNothing()
        {
            var privacy = new PrivacyService(new SettingsModel());
            privacy.Answer(PrivacyChoice.NotAsked);
            privacy.Answer(PrivacyChoice.Declined);
            privacy.Answer(PrivacyChoice.Declined);
            privacy.Answer(PrivacyChoice.NotAsked);

            Assert.AreEqual(PrivacyChoice.Declined, privacy.Choice);
            Assert.AreEqual(1, _changes.Count);
        }

        [Test]
        public void RemoveAdsGate_PlaysTheAdUntilRemoveAdsIsBought()
        {
            var meta = new MetaModel();
            var ads = new MockAdsService(2f);
            var gate = new RemoveAdsGate(ads, new MockIapService(meta, RemoveAds), RemoveAds);

            bool? result = null;
            gate.ShowRewarded(AdPlacements.IncomeBoost, watched => result = watched);
            Assert.IsNull(result, "the reward waits for the video");
            ads.Tick(2.1f);
            Assert.AreEqual(true, result);
            Assert.AreEqual(AdPlacements.IncomeBoost, ads.LastPlacement);
        }

        [Test]
        public void RemoveAdsGate_GrantsAtOnceWithoutAnAdOnceOwned()
        {
            var meta = new MetaModel { removeAdsOwned = true };
            var ads = new MockAdsService(2f);
            var gate = new RemoveAdsGate(ads, new MockIapService(meta, RemoveAds), RemoveAds);

            bool? result = null;
            gate.ShowRewarded(AdPlacements.DailyDouble, watched => result = watched);
            Assert.AreEqual(true, result);
            Assert.IsNull(ads.LastPlacement, "no ad was shown");
            Assert.IsTrue(gate.AdsRemoved && gate.IsRewardedReady);
        }

        [Test]
        public void MockStore_ShowsPriceBuysAndRestores()
        {
            var meta = new MetaModel();
            var iap = new MockIapService(meta, RemoveAds, "$2.99");
            Assert.IsTrue(iap.IsReady);
            Assert.AreEqual("$2.99", iap.PriceText(RemoveAds));
            Assert.AreEqual(string.Empty, iap.PriceText("coins_pack"));

            bool bought = false, restored = false;
            iap.Purchase(RemoveAds, ok => bought = ok);
            iap.Restore(ok => restored = ok);
            Assert.IsTrue(bought && restored && meta.removeAdsOwned && iap.IsOwned(RemoveAds));
        }
    }
}
