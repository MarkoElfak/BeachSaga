using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    /// <summary>Local auth, the mock social service and the visit bonus record.</summary>
    public sealed class OnlineMockTests
    {
        private static readonly DateTime Day = new DateTime(2026, 10, 16);

        [Test]
        public void LocalAuth_CreatesAnIdOnceAndKeepsIt()
        {
            var meta = new MetaModel();
            var first = new LocalAuthService(meta, "You");
            StringAssert.StartsWith("local-", first.PlayerId);
            Assert.AreEqual("You", first.DisplayName);
            Assert.IsTrue(first.IsSignedIn);
            Assert.AreEqual(first.PlayerId, new LocalAuthService(meta, "Other").PlayerId, "same save, same player");
            Assert.AreEqual("You", new LocalAuthService(meta, "Other").DisplayName);
            Assert.AreNotEqual(first.PlayerId, new LocalAuthService(new MetaModel(), "You").PlayerId);
        }

        [Test]
        public void MockSocial_ListsFriendsAndAllowsOneLikeAndGiftEach()
        {
            var social = new MockSocialService(new[] { new FriendInfo("a", "Ann", 2), new FriendInfo("b", "Bo", 3) });
            IReadOnlyList<FriendInfo> friends = null;
            social.GetFriends(list => friends = list);
            Assert.AreEqual(2, friends.Count);
            Assert.AreEqual("Bo", friends[1].DisplayName);

            var results = new List<bool>();
            social.Like("a", results.Add);
            social.Like("a", results.Add);
            social.SendGift("a", results.Add);
            social.SendGift("a", results.Add);
            social.Like(null, results.Add);
            CollectionAssert.AreEqual(new[] { true, false, true, false, false }, results);
            Assert.IsTrue(social.HasLiked("a") && social.HasSentGift("a"));
            Assert.IsFalse(social.HasLiked("b") || social.HasSentGift("b"));
        }

        [Test]
        public void VisitBonus_OncePerIslandPerDay()
        {
            var meta = new MetaModel();
            var ledger = new VisitBonusLedger(meta, 5);
            Assert.IsTrue(ledger.TryClaim("a", Day));
            Assert.IsFalse(ledger.TryClaim("a", Day), "same island, same day");
            Assert.IsTrue(ledger.TryClaim("b", Day));
            Assert.AreEqual(2, ledger.ClaimedToday(Day));
            Assert.IsTrue(ledger.CanClaim("a", Day.AddDays(1)), "a new day starts over");
            Assert.IsTrue(ledger.TryClaim("a", Day.AddDays(1)));
            Assert.AreEqual(1, ledger.ClaimedToday(Day.AddDays(1)));
            Assert.IsFalse(ledger.CanClaim(null, Day));
        }

        [Test]
        public void VisitBonus_HasADailyLimit()
        {
            var ledger = new VisitBonusLedger(new MetaModel(), 2);
            Assert.IsTrue(ledger.TryClaim("a", Day));
            Assert.IsTrue(ledger.TryClaim("b", Day));
            Assert.IsFalse(ledger.TryClaim("c", Day));
            Assert.IsTrue(ledger.TryClaim("c", Day.AddDays(1)));
        }
    }
}
