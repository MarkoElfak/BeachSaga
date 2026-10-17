using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    /// <summary>New season formulas: seashells from earnings and the income multiplier they give.</summary>
    public sealed class SeasonMathTests
    {
        [TestCase(0L, 0L)]
        [TestCase(-5L, 0L)]
        [TestCase(24999L, 0L)]
        [TestCase(25000L, 1L)]
        [TestCase(99999L, 1L)]
        [TestCase(100000L, 2L)]
        [TestCase(2000000L, 8L)]
        [TestCase(2500000L, 10L)]
        public void SeashellsAreTheSquareRootOfEarnedUnits(long earned, long expected)
        {
            Assert.AreEqual(expected, SeasonMath.Seashells(earned, 25000, 0.5f));
        }

        [Test]
        public void LinearExponentPaysOneShellPerUnit()
        {
            Assert.AreEqual(7L, SeasonMath.Seashells(7 * 1000 + 999, 1000, 1f));
        }

        [Test]
        public void ZeroUnitPaysNothing()
        {
            Assert.AreEqual(0L, SeasonMath.Seashells(1000000, 0, 0.5f));
        }

        [Test]
        public void MultiplierGrowsTenPercentPerShell()
        {
            Assert.AreEqual(1f, SeasonMath.IncomeMultiplier(0, 0.1f), 1e-5f);
            Assert.AreEqual(1.8f, SeasonMath.IncomeMultiplier(8, 0.1f), 1e-5f);
            Assert.AreEqual(1f, SeasonMath.IncomeMultiplier(-3, 0.1f), 1e-5f);
        }
    }
}
