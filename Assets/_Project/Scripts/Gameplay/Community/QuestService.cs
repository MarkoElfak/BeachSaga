using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Gives every neighbour one quest at a time and tracks progress from game events (sales, coins earned,
    /// visitors served, decorations placed). Completing a quest pays its reward and assigns the next one.
    /// </summary>
    public sealed class QuestService : IDisposable
    {
        private readonly GameDatabase _database;
        private readonly NeighbourService _neighbours;
        private readonly StandService _stands;
        private readonly IEconomyService _economy;
        private readonly ProgressionService _progression;
        private bool _payingReward;

        public QuestService(GameDatabase database, NeighbourService neighbours, StandService stands,
            IEconomyService economy, ProgressionService progression)
        {
            _database = database;
            _neighbours = neighbours;
            _stands = stands;
            _economy = economy;
            _progression = progression;
            for (int i = 0; i < neighbours.Count; i++)
            {
                if (database.FindQuest(neighbours.Get(i).questId) == null) AssignNext(i);
            }

            EventBus.Subscribe<NeighbourMovedIn>(OnMovedIn);
            EventBus.Subscribe<SaleCompleted>(OnSale);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<VisitorPurchased>(OnVisitorPurchased);
            EventBus.Subscribe<DecorationPlaced>(OnDecorationPlaced);
        }

        public QuestDefinition GetQuest(int neighbourIndex) => _database.FindQuest(_neighbours.Get(neighbourIndex).questId);

        /// <summary>"Sell 5 Donuts" style text for the neighbour's current quest, or empty.</summary>
        public string Describe(int neighbourIndex)
        {
            QuestDefinition quest = GetQuest(neighbourIndex);
            if (quest == null) return string.Empty;
            string product = quest.product != null ? quest.product.displayName : string.Empty;
            return string.Format(quest.description, quest.target, product);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<NeighbourMovedIn>(OnMovedIn);
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<VisitorPurchased>(OnVisitorPurchased);
            EventBus.Unsubscribe<DecorationPlaced>(OnDecorationPlaced);
        }

        private void OnMovedIn(NeighbourMovedIn evt) => AssignNext(evt.NeighbourIndex);

        private void OnSale(SaleCompleted evt) => Advance(QuestGoal.SellProduct, _stands.Get(evt.StandIndex).Product, 1);

        private void OnCoinsChanged(CoinsChanged evt)
        {
            if (evt.Delta > 0 && !_payingReward) Advance(QuestGoal.EarnCoins, null, evt.Delta);
        }

        private void OnVisitorPurchased(VisitorPurchased evt) => Advance(QuestGoal.ServeVisitors, null, 1);

        private void OnDecorationPlaced(DecorationPlaced evt) => Advance(QuestGoal.PlaceDecorations, null, 1);

        private void Advance(QuestGoal goal, ProductDefinition product, long amount)
        {
            for (int i = 0; i < _neighbours.Count; i++)
            {
                QuestDefinition quest = GetQuest(i);
                if (quest == null || quest.goal != goal) continue;
                if (goal == QuestGoal.SellProduct && quest.product != product) continue;

                NeighbourModel neighbour = _neighbours.Get(i);
                neighbour.questProgress = Math.Min(quest.target, neighbour.questProgress + amount);
                EventBus.Publish(new QuestProgressed(i));
                if (neighbour.questProgress >= quest.target) Complete(i, quest);
            }
        }

        private void Complete(int neighbourIndex, QuestDefinition quest)
        {
            _payingReward = true;
            _economy.Earn(quest.rewardCoins);
            _payingReward = false;
            _neighbours.Get(neighbourIndex).questsCompleted++;
            EventBus.Publish(new QuestCompleted(neighbourIndex, quest.rewardCoins));
            AssignNext(neighbourIndex);
        }

        /// <summary>Cycles through the quests available at the current tier, staggered per neighbour.</summary>
        private void AssignNext(int neighbourIndex)
        {
            NeighbourModel neighbour = _neighbours.Get(neighbourIndex);
            neighbour.questId = string.Empty;
            neighbour.questProgress = 0;
            QuestDefinition[] quests = _database.quests;
            if (quests == null || quests.Length == 0) return;

            for (int attempt = 0; attempt < quests.Length; attempt++)
            {
                QuestDefinition quest = quests[(neighbour.questsCompleted + neighbourIndex + attempt) % quests.Length];
                if (quest == null) continue;
                bool sellable = quest.goal != QuestGoal.SellProduct || (quest.product != null && _stands.FindByProduct(quest.product.id) != null);
                if (_progression.IsReached(quest.minTier) && sellable)
                {
                    neighbour.questId = quest.id;
                    break;
                }
            }

            EventBus.Publish(new QuestProgressed(neighbourIndex));
        }
    }
}
