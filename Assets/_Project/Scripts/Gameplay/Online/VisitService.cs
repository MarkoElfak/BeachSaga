using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine.SceneManagement;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Visiting: publishes the player's own snapshot, loads the chosen one (validated), pays the visit bonus
    /// (once per island per day), opens the read-only Island scene and brings the player home again. The home
    /// beach keeps running meanwhile; its sales and rent are banked straight into the wallet.
    /// </summary>
    public sealed class VisitService : IDisposable
    {
        private readonly GameDatabase _database;
        private readonly IAuthService _auth;
        private readonly IIslandRepository _islands;
        private readonly ISocialService _social;
        private readonly Func<IslandSnapshot> _captureHome;
        private readonly VisitBonusLedger _ledger;
        private readonly ITimeProvider _clock;
        private readonly Func<double> _incomePerSecond;
        private readonly IEconomyService _economy;
        private readonly IAnalyticsService _analytics;
        private readonly Action<string> _loadScene;
        private bool _loading;

        public VisitService(GameDatabase database, IAuthService auth, IIslandRepository islands, ISocialService social,
            Func<IslandSnapshot> captureHome, VisitBonusLedger ledger, ITimeProvider clock, Func<double> incomePerSecond,
            IEconomyService economy, IAnalyticsService analytics, Action<string> loadScene = null)
        {
            _database = database;
            _auth = auth;
            _islands = islands;
            _social = social;
            _captureHome = captureHome;
            _ledger = ledger;
            _clock = clock;
            _incomePerSecond = incomePerSecond;
            _economy = economy;
            _analytics = analytics;
            _loadScene = loadScene ?? (scene => SceneManager.LoadScene(scene));
            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Subscribe<RentDue>(OnRentDue);
        }

        /// <summary>The island being visited (validated), or null at home.</summary>
        public IslandSnapshot Current { get; private set; }
        public bool IsVisiting => Current != null;
        public bool IsLoading => _loading;
        public long LastBonus { get; private set; }
        public string PlayerId => _auth.PlayerId;
        public bool IsOwnIsland => Current != null && Current.ownerId == _auth.PlayerId;

        /// <summary>Coins earned at home while away on the current visit.</summary>
        public long EarnedWhileAway { get; private set; }

        public long BonusCoins => DailyRewardCalendar.Coins(_incomePerSecond(), _database.online.bonusIncomeMinutes,
            _database.online.bonusMinimumCoins);

        public bool CanClaimBonus(string ownerId) => _ledger.CanClaim(ownerId, Today);

        private DateTime Today => _clock.UtcNow.ToLocalTime().Date;

        /// <summary>Stores the player's island as it is right now (what friends would see).</summary>
        public void PublishHome(Action<bool> onDone = null)
        {
            if (!IsVisiting) _islands.SaveSnapshot(_captureHome(), onDone);
        }

        public void StartVisit(string ownerId)
        {
            if (IsVisiting || _loading || string.IsNullOrEmpty(ownerId)) return;
            _loading = true;
            PublishHome(saved => _islands.LoadSnapshot(ownerId, raw => OnLoaded(ownerId, raw)));
        }

        public void ReturnHome()
        {
            if (!IsVisiting) return;
            Current = null;
            LastBonus = 0;
            _loadScene(SceneNames.Beach);
        }

        public void Like()
        {
            if (Current == null || IsOwnIsland) return;
            string ownerId = Current.ownerId;
            _social.Like(ownerId, ok => Finish(false, ok, AnalyticsEvents.IslandLiked, ownerId));
        }

        public void SendGift()
        {
            if (Current == null || IsOwnIsland) return;
            string ownerId = Current.ownerId;
            _social.SendGift(ownerId, ok => Finish(true, ok, AnalyticsEvents.GiftSent, ownerId));
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Unsubscribe<RentDue>(OnRentDue);
        }

        private void OnLoaded(string ownerId, IslandSnapshot raw)
        {
            _loading = false;
            IslandSnapshot snapshot = IslandSnapshotValidator.Sanitize(raw, _database);
            if (snapshot == null)
            {
                EventBus.Publish(new VisitFailed(ownerId));
                return;
            }

            EventBus.Publish(new LeavingHome());
            Current = snapshot;
            EarnedWhileAway = 0;
            LastBonus = _ledger.TryClaim(snapshot.ownerId, Today) ? BonusCoins : 0;
            if (LastBonus > 0) _economy.Earn(LastBonus);
            _analytics.LogEvent(AnalyticsEvents.IslandVisited, "tier", snapshot.tier);
            EventBus.Publish(new VisitStarted(snapshot.ownerId, LastBonus));
            _loadScene(SceneNames.Island);
        }

        private void Finish(bool gift, bool success, string analyticsEvent, string ownerId)
        {
            if (success) _analytics.LogEvent(analyticsEvent, "owner", ownerId);
            EventBus.Publish(new SocialActionDone(gift, success));
        }

        // Nobody is on the beach to tap the coins, so the income goes straight into the wallet.
        private void OnSaleCompleted(SaleCompleted evt) => BankWhileAway(evt.Amount);
        private void OnRentDue(RentDue evt) => BankWhileAway(evt.Amount);

        private void BankWhileAway(long amount)
        {
            if (!IsVisiting || amount <= 0) return;
            _economy.Earn(amount);
            EarnedWhileAway += amount;
        }
    }
}
