using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Which islands already paid a visit bonus today (local calendar day): at most one per island and
    /// maxPerDay in total. The list starts over on a new day.
    /// </summary>
    public sealed class VisitBonusLedger
    {
        private readonly MetaModel _meta;
        private readonly int _maxPerDay;

        public VisitBonusLedger(MetaModel meta, int maxPerDay)
        {
            _meta = meta;
            _maxPerDay = maxPerDay;
        }

        public int ClaimedToday(DateTime today) => IsToday(today) ? _meta.visitBonusOwners.Count : 0;

        public bool CanClaim(string ownerId, DateTime today)
        {
            if (string.IsNullOrEmpty(ownerId) || ClaimedToday(today) >= _maxPerDay) return false;
            return !IsToday(today) || !_meta.visitBonusOwners.Contains(ownerId);
        }

        /// <summary>Records the bonus; false (and nothing recorded) when it is not allowed.</summary>
        public bool TryClaim(string ownerId, DateTime today)
        {
            if (!CanClaim(ownerId, today)) return false;
            if (!IsToday(today))
            {
                _meta.visitBonusDay = DailyRewardCalendar.FormatDay(today);
                _meta.visitBonusOwners.Clear();
            }

            _meta.visitBonusOwners.Add(ownerId);
            return true;
        }

        private bool IsToday(DateTime today) => _meta.visitBonusDay == DailyRewardCalendar.FormatDay(today);
    }
}
