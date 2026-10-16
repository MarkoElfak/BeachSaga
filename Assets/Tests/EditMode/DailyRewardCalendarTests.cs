using System;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class DailyRewardCalendarTests
    {
        private const int Cycle = 7;
        private static readonly DateTime Monday = new DateTime(2026, 7, 6);

        [Test]
        public void NeverClaimed_CanClaimDayOne()
        {
            Assert.IsTrue(DailyRewardCalendar.CanClaim(null, Monday));
            Assert.AreEqual(0, DailyRewardCalendar.DayIndex(null, 0, Monday, Cycle));
            Assert.AreEqual(1, DailyRewardCalendar.StreakAfterClaim(null, 0, Monday));
            Assert.AreEqual(0, DailyRewardCalendar.CurrentStreak(null, 0, Monday));
        }

        [Test]
        public void SameDay_CannotClaimAgain_AndShowsTheClaimedDay()
        {
            Assert.IsFalse(DailyRewardCalendar.CanClaim(Monday, Monday.AddHours(23)));
            Assert.AreEqual(2, DailyRewardCalendar.DayIndex(Monday, 3, Monday, Cycle));
            Assert.AreEqual(3, DailyRewardCalendar.CurrentStreak(Monday, 3, Monday));
        }

        [Test]
        public void NextDay_ContinuesTheStreak()
        {
            DateTime tuesday = Monday.AddDays(1);
            Assert.IsTrue(DailyRewardCalendar.CanClaim(Monday, tuesday));
            Assert.IsTrue(DailyRewardCalendar.ContinuesStreak(Monday, tuesday));
            Assert.AreEqual(3, DailyRewardCalendar.CurrentStreak(Monday, 3, tuesday), "the streak is alive until the day is missed");
            Assert.AreEqual(4, DailyRewardCalendar.StreakAfterClaim(Monday, 3, tuesday));
            Assert.AreEqual(3, DailyRewardCalendar.DayIndex(Monday, 3, tuesday, Cycle));
        }

        [Test]
        public void MissedDay_ResetsToDayOne()
        {
            DateTime wednesday = Monday.AddDays(2);
            Assert.IsTrue(DailyRewardCalendar.CanClaim(Monday, wednesday));
            Assert.IsFalse(DailyRewardCalendar.ContinuesStreak(Monday, wednesday));
            Assert.AreEqual(0, DailyRewardCalendar.CurrentStreak(Monday, 5, wednesday));
            Assert.AreEqual(1, DailyRewardCalendar.StreakAfterClaim(Monday, 5, wednesday));
            Assert.AreEqual(0, DailyRewardCalendar.DayIndex(Monday, 5, wednesday, Cycle));
        }

        [Test]
        public void AfterTheLastDay_TheCycleStartsOver()
        {
            DateTime next = Monday.AddDays(1);
            Assert.AreEqual(6, DailyRewardCalendar.DayIndex(Monday, 6, next, Cycle), "7th claim pays the jackpot");
            Assert.AreEqual(0, DailyRewardCalendar.DayIndex(Monday, 7, next, Cycle), "8th claim is day 1 again");
            Assert.AreEqual(8, DailyRewardCalendar.StreakAfterClaim(Monday, 7, next), "the streak itself keeps counting");
        }

        [Test]
        public void ClockMovedBack_NeverAllowsASecondClaim()
        {
            Assert.IsFalse(DailyRewardCalendar.CanClaim(Monday, Monday.AddDays(-3)));
        }

        [Test]
        public void SavedDay_RoundTrips_AndBadValuesMeanNeverClaimed()
        {
            string saved = DailyRewardCalendar.FormatDay(Monday.AddHours(15));
            Assert.AreEqual("2026-07-06", saved);
            Assert.AreEqual(Monday, DailyRewardCalendar.ParseDay(saved));
            Assert.IsNull(DailyRewardCalendar.ParseDay(string.Empty));
            Assert.IsNull(DailyRewardCalendar.ParseDay(null));
            Assert.IsNull(DailyRewardCalendar.ParseDay("not a date"));
        }

        [Test]
        public void Coins_AreMinutesOfIncome_ButNeverBelowTheMinimum()
        {
            Assert.AreEqual(600L, DailyRewardCalendar.Coins(2.0, 5f, 100));
            Assert.AreEqual(100L, DailyRewardCalendar.Coins(0.1, 5f, 100));
            Assert.AreEqual(100L, DailyRewardCalendar.Coins(-4.0, 5f, 100));
            Assert.AreEqual(90L, DailyRewardCalendar.Coins(1.01, 1.5f, 0), "90.9 is floored");
        }

        [Test]
        public void EmptyCycle_IsSafe()
        {
            Assert.AreEqual(0, DailyRewardCalendar.DayIndex(Monday, 4, Monday.AddDays(1), 0));
        }
    }
}
