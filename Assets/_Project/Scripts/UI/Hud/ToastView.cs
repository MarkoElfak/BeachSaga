using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Short messages that slide in and fade: quest done, new neighbour, new building, house info.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        private const float ShowSeconds = 2.6f;
        private const float FadeSeconds = 0.35f;

        [SerializeField] private CanvasGroup group = null;
        [SerializeField] private TMP_Text label = null;

        private readonly Queue<string> _queue = new Queue<string>();
        private float _timer;
        private GameDatabase _database;

        private void Start()
        {
            group.alpha = 0f;
            if (!ServiceLocator.TryGet(out _database)) return;
            EventBus.Subscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Subscribe<NeighbourMovedIn>(OnMovedIn);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<HouseTapped>(OnHouseTapped);
            EventBus.Subscribe<ProductUnlocked>(OnProductUnlocked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Unsubscribe<NeighbourMovedIn>(OnMovedIn);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<HouseTapped>(OnHouseTapped);
            EventBus.Unsubscribe<ProductUnlocked>(OnProductUnlocked);
        }

        public void Show(string message)
        {
            _queue.Enqueue(message);
        }

        private void Update()
        {
            if (_timer <= 0f)
            {
                if (_queue.Count == 0) return;
                label.text = _queue.Dequeue();
                _timer = ShowSeconds;
            }

            _timer -= Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01((ShowSeconds - _timer) / FadeSeconds);
            float fadeOut = Mathf.Clamp01(_timer / FadeSeconds);
            group.alpha = Mathf.Min(fadeIn, fadeOut);
        }

        private void OnQuestCompleted(QuestCompleted evt)
        {
            NeighbourModel neighbour = ServiceLocator.Get<NeighbourService>().Get(evt.NeighbourIndex);
            Show($"{neighbour.name}'s request done!  +{NumberFormatter.Format(evt.Reward)}");
        }

        private void OnMovedIn(NeighbourMovedIn evt) =>
            Show($"{ServiceLocator.Get<NeighbourService>().Get(evt.NeighbourIndex).name} is moving in!");

        private void OnTierUpgraded(TierUpgraded evt)
        {
            if (evt.LotIndex != BuildingManager.MainLotIndex || evt.Tier <= 1) return;
            TierDefinition tier = _database.GetTier(evt.Tier);
            if (tier != null) Show($"Your {tier.displayName} is open!");
        }

        private void OnProductUnlocked(ProductUnlocked evt)
        {
            ProductDefinition product = _database.FindProduct(evt.ProductId);
            if (product != null) Show($"Now selling {product.displayName}");
        }

        private void OnHouseTapped(HouseTapped evt)
        {
            var neighbours = ServiceLocator.Get<NeighbourService>();
            int index = neighbours.FindByLot(evt.LotIndex);
            if (index < 0) return;
            NeighbourModel neighbour = neighbours.Get(index);
            string quest = ServiceLocator.Get<QuestService>().Describe(index);
            Show(string.IsNullOrEmpty(quest) ? $"{neighbour.name}'s house" : $"{neighbour.name}: {quest}  ({neighbour.questProgress} done)");
        }
    }
}
