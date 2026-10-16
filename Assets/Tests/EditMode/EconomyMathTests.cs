using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class EconomyMathTests
    {
        [TestCase(10L, 1f, 0, 0.25f, ExpectedResult = 10L)]
        [TestCase(10L, 1f, 2, 0.25f, ExpectedResult = 15L)]
        [TestCase(5L, 1.5f, 0, 0f, ExpectedResult = 8L)] // 7.5 rounds away from zero
        [TestCase(40L, 1.2f, 3, 0.1f, ExpectedResult = 62L)] // 40 * 1.2 * 1.3 = 62.4
        [TestCase(10L, -1f, 0, 0f, ExpectedResult = 0L)] // never negative
        public long SaleValue_AppliesSpendAndLevelBonus(long price, float spend, int level, float bonus)
        {
            return EconomyMath.SaleValue(price, spend, level, bonus);
        }

        [TestCase(100L, 1.5f, 0, ExpectedResult = 100L)]
        [TestCase(100L, 1.5f, 2, ExpectedResult = 225L)]
        [TestCase(100L, 1.5f, -3, ExpectedResult = 100L)] // negative level counts as 0
        [TestCase(50L, 2f, 10, ExpectedResult = 51200L)]
        public long UpgradeCost_GrowsGeometrically(long baseCost, float growth, int level)
        {
            return EconomyMath.UpgradeCost(baseCost, growth, level);
        }

        [Test]
        public void UpgradeCost_IsStrictlyIncreasingForGrowthAboveOne()
        {
            long previous = 0;
            for (int level = 0; level < 30; level++)
            {
                long cost = EconomyMath.UpgradeCost(25, 1.35f, level);
                Assert.Greater(cost, previous, $"level {level}");
                previous = cost;
            }
        }

        [TestCase(101L, 2f, ExpectedResult = 202L)]
        [TestCase(3L, 0.5f, ExpectedResult = 2L)] // 1.5 rounds away from zero
        [TestCase(7L, 0f, ExpectedResult = 0L)]
        [TestCase(7L, -2f, ExpectedResult = 0L)]
        public long ApplyMultiplier_RoundsToWholeCoins(long amount, float multiplier)
        {
            return EconomyMath.ApplyMultiplier(amount, multiplier);
        }
    }
}
