using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class EconomyServiceTests
    {
        private int _events;
        private CoinsChanged _last;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _events = 0;
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
        }

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        private void OnCoinsChanged(CoinsChanged evt)
        {
            _events++;
            _last = evt;
        }

        [Test]
        public void Earn_AddsCoinsAndPublishes()
        {
            var wallet = new WalletModel(10);
            var economy = new EconomyService(wallet);

            economy.Earn(15);

            Assert.AreEqual(25, economy.Coins);
            Assert.AreEqual(25, wallet.coins, "the serializable model is the source of truth");
            Assert.AreEqual(1, _events);
            Assert.AreEqual(15, _last.Delta);
        }

        [Test]
        public void Earn_IgnoresZeroAndNegative()
        {
            var economy = new EconomyService(new WalletModel(10));
            economy.Earn(0);
            economy.Earn(-5);
            Assert.AreEqual(10, economy.Coins);
            Assert.AreEqual(0, _events);
        }

        [Test]
        public void TrySpend_FailsWhenTooPoorAndKeepsBalance()
        {
            var economy = new EconomyService(new WalletModel(10));
            Assert.IsFalse(economy.TrySpend(11));
            Assert.AreEqual(10, economy.Coins);
            Assert.AreEqual(0, _events);
        }

        [Test]
        public void TrySpend_SpendsExactBalance()
        {
            var economy = new EconomyService(new WalletModel(10));
            Assert.IsTrue(economy.TrySpend(10));
            Assert.AreEqual(0, economy.Coins);
            Assert.AreEqual(-10, _last.Delta);
        }

        [Test]
        public void TrySpend_RejectsNegativeAndAllowsZero()
        {
            var economy = new EconomyService(new WalletModel(10));
            Assert.IsFalse(economy.TrySpend(-1));
            Assert.IsTrue(economy.TrySpend(0));
            Assert.AreEqual(10, economy.Coins);
            Assert.AreEqual(0, _events);
        }
    }
}
