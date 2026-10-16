using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class AchievementBookTests
    {
        private AchievementTestData _data;
        private List<AchievementModel> _saved;
        private readonly List<int> _completed = new List<int>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _data = new AchievementTestData();
            _saved = new List<AchievementModel>();
            _completed.Clear();
            EventBus.Subscribe<AchievementCompleted>(evt => _completed.Add(evt.Index));
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            _data.Dispose();
        }

        private AchievementBook Book(params AchievementDefinition[] definitions) => new AchievementBook(definitions, _saved);

        [Test]
        public void Counters_AddUp_CapAtTheTarget_AndCompleteOnce()
        {
            AchievementBook book = Book(_data.Make("coins", AchievementGoal.EarnCoins, 100));

            book.Add(AchievementGoal.EarnCoins, null, 60);
            Assert.IsFalse(book.IsComplete(0));
            Assert.AreEqual(0.6f, book.Fraction(0), 0.001f);
            book.Add(AchievementGoal.EarnCoins, null, 60);
            book.Add(AchievementGoal.EarnCoins, null, 60);

            Assert.AreEqual(100, book.Progress(0), "progress stops at the target");
            Assert.IsTrue(book.IsComplete(0) && book.CanClaim(0));
            CollectionAssert.AreEqual(new[] { 0 }, _completed, "the completion event fires once");
        }

        [Test]
        public void SellProduct_CountsOnlyItsProduct_OrEveryProductWhenEmpty()
        {
            AchievementBook book = Book(
                _data.Make("donuts", AchievementGoal.SellProduct, 10, product: _data.Donuts),
                _data.Make("anything", AchievementGoal.SellProduct, 10),
                _data.Make("coins", AchievementGoal.EarnCoins, 10000, description: "Earn {0} coins"));

            book.Add(AchievementGoal.SellProduct, "donuts", 1);
            book.Add(AchievementGoal.SellProduct, "towels", 1);

            Assert.AreEqual(1, book.Progress(0));
            Assert.AreEqual(2, book.Progress(1));
            Assert.AreEqual("Do 10 Donuts", book.Describe(0));
            Assert.AreEqual("Earn 10,000 coins", book.Describe(2));
        }

        [Test]
        public void Levels_KeepTheBestValue_AndIgnoreCounterReports()
        {
            AchievementBook book = Book(_data.Make("hotel", AchievementGoal.ReachTier, 4));

            book.ReachLevel(AchievementGoal.ReachTier, 3);
            book.ReachLevel(AchievementGoal.ReachTier, 2);
            book.Add(AchievementGoal.ReachTier, null, 5);

            Assert.AreEqual(3, book.Progress(0), "levels never go down and are not summed");
            book.ReachLevel(AchievementGoal.ReachTier, 4);
            Assert.IsTrue(book.IsComplete(0));
        }

        [Test]
        public void Claim_WorksOnceAndOnlyWhenComplete()
        {
            AchievementBook book = Book(_data.Make("visits", AchievementGoal.VisitIslands, 2));

            Assert.IsFalse(book.TryClaim(0), "not complete yet");
            book.Add(AchievementGoal.VisitIslands, null, 2);
            Assert.AreEqual(1, book.ClaimableCount);
            Assert.IsTrue(book.TryClaim(0));
            Assert.IsFalse(book.TryClaim(0), "second claim");
            Assert.IsFalse(book.TryClaim(5), "unknown index");
            Assert.AreEqual(0, book.ClaimableCount);
            Assert.AreEqual(1, book.CompletedCount);
            Assert.IsTrue(book.IsClaimed(0));
        }

        [Test]
        public void SavedProgress_IsMatchedById_AndNewAchievementsAreAdded()
        {
            _saved.Add(new AchievementModel { id = "quests", progress = 4, claimed = false });
            _saved.Add(new AchievementModel { id = "removed", progress = 9 });

            AchievementBook book = Book(
                _data.Make("streak", AchievementGoal.DailyStreak, 7),
                _data.Make("quests", AchievementGoal.CompleteQuests, 5));

            Assert.AreEqual(0, book.Progress(0));
            Assert.AreEqual(4, book.Progress(1), "progress follows the id, not the position");
            Assert.AreEqual(3, _saved.Count, "the new achievement gets a saved entry");
            Assert.AreEqual(1, book.IndexOf("quests"));
            Assert.AreEqual(-1, book.IndexOf("removed"));
        }

        [Test]
        public void MissingDefinitions_AreHarmless()
        {
            var book = new AchievementBook(null, _saved);
            book.Add(AchievementGoal.EarnCoins, null, 10);
            Assert.AreEqual(0, book.Count);
            Assert.AreEqual(0, book.ClaimableCount);
        }
    }
}
