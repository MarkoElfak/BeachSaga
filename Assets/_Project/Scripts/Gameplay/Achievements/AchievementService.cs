using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Feeds game events into the AchievementBook and pays rewards (coins and 2x boost minutes) when the
    /// player claims a completed achievement. Level goals read the island's current state.
    /// </summary>
    public sealed class AchievementService : IDisposable
    {
        private readonly MetaModel _meta;
        private readonly Func<int, string> _productOfStand;
        private readonly Func<AchievementGoal, long> _readLevel;
        private readonly IEconomyService _economy;
        private readonly IncomeBoost _boost;
        private readonly IAnalyticsService _analytics;
        private bool _payingReward;

        public AchievementService(AchievementBook book, MetaModel meta, Func<int, string> productOfStand,
            Func<AchievementGoal, long> readLevel, IEconomyService economy, IncomeBoost boost, IAnalyticsService analytics)
        {
            Book = book;
            _meta = meta;
            _productOfStand = productOfStand;
            _readLevel = readLevel;
            _economy = economy;
            _boost = boost;
            _analytics = analytics;
            SyncLevels();

            EventBus.Subscribe<SaleCompleted>(OnSale);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<VisitorPurchased>(OnVisitorPurchased);
            EventBus.Subscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Subscribe<VisitStarted>(OnVisitStarted);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Subscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Subscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Subscribe<DailyRewardClaimed>(OnDailyClaimed);
        }

        public AchievementBook Book { get; }

        /// <summary>Wires the service to the real island services.</summary>
        public static AchievementService Create(GameDatabase database, GameSaveData data, BuildingManager buildings,
            StandService stands, NeighbourService neighbours, IEconomyService economy, IncomeBoost boost,
            IAnalyticsService analytics)
        {
            var book = new AchievementBook(database.achievements, data.achievements);
            var levels = new AchievementLevels(buildings, stands, neighbours, data);
            return new AchievementService(book, data.meta, index => stands.Get(index).Product.id, levels.Read,
                economy, boost, analytics);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<VisitorPurchased>(OnVisitorPurchased);
            EventBus.Unsubscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Unsubscribe<VisitStarted>(OnVisitStarted);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Unsubscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Unsubscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Unsubscribe<DailyRewardClaimed>(OnDailyClaimed);
        }

        /// <summary>Pays a completed achievement's reward once. Returns false if it is not claimable.</summary>
        public bool Claim(int index)
        {
            if (!Book.TryClaim(index)) return false;
            AchievementDefinition definition = Book.Get(index);
            _payingReward = true;
            _economy.Earn(definition.rewardCoins);
            _payingReward = false;
            _boost.Extend(definition.rewardBoostMinutes * 60f);
            _analytics.LogEvent(AnalyticsEvents.AchievementClaimed, "achievement", definition.id);
            EventBus.Publish(new SoundRequested(SoundId.Reward));
            EventBus.Publish(new AchievementClaimed(index, definition.rewardCoins, definition.rewardBoostMinutes));
            return true;
        }

        private void SyncLevels()
        {
            Sync(AchievementGoal.ReachTier);
            Sync(AchievementGoal.HostNeighbours);
            Sync(AchievementGoal.OwnDecorations);
            Sync(AchievementGoal.StandLevel);
            Sync(AchievementGoal.DailyStreak);
        }

        private void Sync(AchievementGoal goal) => Book.ReachLevel(goal, _readLevel(goal));

        private void OnSale(SaleCompleted evt) => Book.Add(AchievementGoal.SellProduct, _productOfStand(evt.StandIndex), 1);

        // Achievement rewards themselves do not count towards "earn coins".
        private void OnCoinsChanged(CoinsChanged evt)
        {
            if (evt.Delta > 0 && !_payingReward) Book.Add(AchievementGoal.EarnCoins, null, evt.Delta);
        }

        private void OnVisitorPurchased(VisitorPurchased evt) => Book.Add(AchievementGoal.ServeVisitors, null, 1);
        private void OnQuestCompleted(QuestCompleted evt) => Book.Add(AchievementGoal.CompleteQuests, null, 1);

        // Visiting your own snapshot is a debug view, not a trip.
        private void OnVisitStarted(VisitStarted evt)
        {
            if (evt.OwnerId != _meta.playerId) Book.Add(AchievementGoal.VisitIslands, null, 1);
        }

        private void OnTierUpgraded(TierUpgraded evt) => Sync(AchievementGoal.ReachTier);
        private void OnNeighbourMovedIn(NeighbourMovedIn evt) => Sync(AchievementGoal.HostNeighbours);
        private void OnDecorationPlaced(DecorationPlaced evt) => Sync(AchievementGoal.OwnDecorations);
        private void OnStandUpgraded(StandUpgraded evt) => Sync(AchievementGoal.StandLevel);
        private void OnDailyClaimed(DailyRewardClaimed evt) => Sync(AchievementGoal.DailyStreak);
    }
}
