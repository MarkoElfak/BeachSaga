using System;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class OfflineEarningsTests
    {
        private const float CapHours = 8f;
        private const float MinSeconds = 60f;

        [Test]
        public void ShortAbsence_EarnsNothing()
        {
            Assert.AreEqual(0.0, OfflineEarnings.CappedSeconds(TimeSpan.FromSeconds(59), CapHours, MinSeconds));
            Assert.AreEqual(0L, OfflineEarnings.Calculate(10.0, TimeSpan.FromSeconds(59), 1f, CapHours, MinSeconds));
        }

        [Test]
        public void ClockMovedBack_EarnsNothing()
        {
            Assert.AreEqual(0L, OfflineEarnings.Calculate(10.0, TimeSpan.FromHours(-3), 1f, CapHours, 0f));
        }

        [Test]
        public void LongAbsence_IsCappedAtCapHours()
        {
            double seconds = OfflineEarnings.CappedSeconds(TimeSpan.FromHours(20), CapHours, MinSeconds);
            Assert.AreEqual(CapHours * 3600.0, seconds, 1e-6);
        }

        [Test]
        public void Earnings_AreRateTimesSecondsTimesEfficiency()
        {
            long coins = OfflineEarnings.Calculate(2.0, TimeSpan.FromHours(1), 0.5f, CapHours, MinSeconds);
            Assert.AreEqual(3600L, coins);
        }

        [Test]
        public void Earnings_AreFlooredAndNeverNegative()
        {
            Assert.AreEqual(61L, OfflineEarnings.Calculate(1.01, TimeSpan.FromSeconds(61), 1f, CapHours, MinSeconds));
            Assert.AreEqual(0L, OfflineEarnings.Calculate(-5.0, TimeSpan.FromHours(1), 1f, CapHours, MinSeconds));
        }

        [Test]
        public void CappedEarnings_DoNotGrowPastTheCap()
        {
            long atCap = OfflineEarnings.Calculate(3.0, TimeSpan.FromHours(CapHours), 0.5f, CapHours, MinSeconds);
            long wayPast = OfflineEarnings.Calculate(3.0, TimeSpan.FromDays(30), 0.5f, CapHours, MinSeconds);
            Assert.AreEqual(atCap, wayPast);
        }
    }
}
