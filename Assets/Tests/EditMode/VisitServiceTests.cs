using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class VisitServiceTests
    {
        private sealed class FixedClock : ITimeProvider
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        }

        private sealed class MemoryRepository : IIslandRepository
        {
            public readonly Dictionary<string, IslandSnapshot> Islands = new Dictionary<string, IslandSnapshot>();

            public void SaveSnapshot(IslandSnapshot snapshot, Action<bool> onDone)
            {
                Islands[snapshot.ownerId] = snapshot;
                onDone?.Invoke(true);
            }

            public void LoadSnapshot(string ownerId, Action<IslandSnapshot> onLoaded) =>
                onLoaded(Islands.TryGetValue(ownerId, out IslandSnapshot found) ? found : null);
        }

        private OnlineTestData _data;
        private MetaModel _meta;
        private WalletModel _wallet;
        private FixedClock _clock;
        private MemoryRepository _islands;
        private MockSocialService _social;
        private MockAnalyticsService _analytics;
        private VisitService _visits;
        private readonly List<string> _scenes = new List<string>();
        private int _failures;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _data = new OnlineTestData();
            _meta = new MetaModel();
            _wallet = new WalletModel(0);
            _clock = new FixedClock();
            _islands = new MemoryRepository();
            _islands.Islands["kai"] = new IslandSnapshot { ownerId = "kai", ownerName = "Kai", tier = 2 };
            _social = new MockSocialService(new[] { new FriendInfo("kai", "Kai", 2) });
            _analytics = new MockAnalyticsService(false);
            _scenes.Clear();
            _failures = 0;
            EventBus.Subscribe<VisitFailed>(_ => _failures++);
            var auth = new LocalAuthService(_meta, "You");
            _visits = new VisitService(_data.Database, auth, _islands, _social,
                () => new IslandSnapshot { ownerId = auth.PlayerId, ownerName = "You", tier = 1 },
                new VisitBonusLedger(_meta, 5), _clock, () => 1.0, new EconomyService(_wallet), _analytics, _scenes.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _visits.Dispose();
            _data.Dispose();
            EventBus.ClearAll();
        }

        [Test]
        public void Visit_LoadsTheIslandScene_AndPaysTheBonusOncePerDay()
        {
            _visits.StartVisit("kai");
            Assert.IsTrue(_visits.IsVisiting);
            Assert.AreEqual("Kai", _visits.Current.ownerName);
            CollectionAssert.AreEqual(new[] { SceneNames.Island }, _scenes);
            long bonus = _visits.LastBonus;
            Assert.AreEqual(Math.Max(_data.Database.online.bonusMinimumCoins, (long)(1.0 * _data.Database.online.bonusIncomeMinutes * 60)), bonus);
            Assert.AreEqual(bonus, _wallet.coins);
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.IslandVisited));

            _visits.ReturnHome();
            Assert.IsFalse(_visits.IsVisiting);
            Assert.AreEqual(SceneNames.Beach, _scenes[1]);

            _visits.StartVisit("kai");
            Assert.AreEqual(0, _visits.LastBonus, "same island, same day");
            Assert.AreEqual(bonus, _wallet.coins);
            _visits.ReturnHome();
            _clock.UtcNow = _clock.UtcNow.AddDays(1);
            _visits.StartVisit("kai");
            Assert.AreEqual(bonus, _visits.LastBonus, "next day pays again");
        }

        [Test]
        public void Visit_PublishesTheOwnIslandFirst_SoItCanBeVisited()
        {
            _visits.StartVisit(_visits.PlayerId);
            Assert.IsTrue(_islands.Islands.ContainsKey(_visits.PlayerId));
            Assert.IsTrue(_visits.IsOwnIsland);
            _visits.Like();
            _visits.SendGift();
            Assert.AreEqual(0, _social.LikesGiven + _social.GiftsSent, "no likes or gifts on your own island");
        }

        [Test]
        public void MissingIsland_StaysHome()
        {
            _visits.StartVisit("ghost");
            Assert.IsFalse(_visits.IsVisiting);
            Assert.IsFalse(_visits.IsLoading);
            Assert.AreEqual(1, _failures);
            Assert.IsEmpty(_scenes);
            Assert.AreEqual(0, _wallet.coins);
        }

        [Test]
        public void LikeAndGift_GoThroughOnce_AndAreLogged()
        {
            _visits.StartVisit("kai");
            _visits.Like();
            _visits.Like();
            _visits.SendGift();
            Assert.IsTrue(_social.HasLiked("kai") && _social.HasSentGift("kai"));
            Assert.AreEqual(1, _social.LikesGiven);
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.IslandLiked));
            Assert.IsTrue(_analytics.HasLogged(AnalyticsEvents.GiftSent));
        }

        [Test]
        public void HomeIncome_IsBankedOnlyWhileAway()
        {
            EventBus.Publish(new SaleCompleted(0, 30, -1));
            Assert.AreEqual(0, _wallet.coins, "at home the coins drop on the beach instead");
            _visits.StartVisit("kai");
            long afterBonus = _wallet.coins;
            EventBus.Publish(new SaleCompleted(0, 30, -1));
            EventBus.Publish(new RentDue(2, 12));
            Assert.AreEqual(afterBonus + 42, _wallet.coins);
            Assert.AreEqual(42, _visits.EarnedWhileAway);
        }
    }
}
