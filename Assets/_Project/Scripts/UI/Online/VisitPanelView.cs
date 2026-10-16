using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Beach HUD "Visit" button and list: the player's own saved snapshot (debug: see your island as a visitor
    /// would) and every friend's island. Picking a row starts the visit.
    /// </summary>
    public sealed class VisitPanelView : MonoBehaviour
    {
        private const float RefreshSeconds = 1f;

        [SerializeField] private Button openButton = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Button[] rowButtons = null;
        [SerializeField] private TMP_Text[] rowLabels = null;
        [SerializeField] private TMP_Text body = null;
        [SerializeField] private Button closeButton = null;

        private readonly List<string> _owners = new List<string>();
        private VisitService _visits;
        private ISocialService _social;
        private TutorialService _tutorial;
        private GameDatabase _database;
        private float _refreshTimer;

        public bool IsOpen => root.activeSelf;

        /// <summary>Owner ids in row order (row 0 is the player's own island).</summary>
        public IReadOnlyList<string> Owners => _owners;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _visits))
            {
                openButton.gameObject.SetActive(false);
                return;
            }

            _social = ServiceLocator.Get<ISocialService>();
            _tutorial = ServiceLocator.Get<TutorialService>();
            _database = ServiceLocator.Get<GameDatabase>();
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            for (int i = 0; i < rowButtons.Length; i++)
            {
                int row = i;
                rowButtons[i].onClick.AddListener(() => Visit(row));
            }

            EventBus.Subscribe<VisitFailed>(OnVisitFailed);
            RefreshButton();
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            foreach (Button button in rowButtons) button.onClick.RemoveAllListeners();
            EventBus.Unsubscribe<VisitFailed>(OnVisitFailed);
        }

        // The button appears once the tutorial is over.
        private void Update()
        {
            if (_visits == null) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshSeconds;
            RefreshButton();
        }

        public void Open()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _visits.PublishHome(); // the "my island" row shows the island as it is now
            body.text = "Visit a friend's beach. Each island pays a small bonus once a day.";
            root.SetActive(true);
            _social.GetFriends(ShowRows);
        }

        public void Close() => root.SetActive(false);

        private void RefreshButton()
        {
            bool available = !_tutorial.IsActive;
            if (openButton.gameObject.activeSelf != available) openButton.gameObject.SetActive(available);
        }

        private void ShowRows(IReadOnlyList<FriendInfo> friends)
        {
            _owners.Clear();
            _owners.Add(_visits.PlayerId);
            foreach (FriendInfo friend in friends) _owners.Add(friend.OwnerId);

            for (int i = 0; i < rowButtons.Length; i++)
            {
                bool used = i < _owners.Count;
                rowButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                string bonus = _visits.CanClaimBonus(_owners[i]) ? $"  +{NumberFormatter.Format(_visits.BonusCoins)}" : string.Empty;
                string name = i == 0 ? "My island (saved snapshot)" : $"{friends[i - 1].DisplayName} - {TierName(friends[i - 1].Tier)}";
                rowLabels[i].text = name + bonus;
                rowButtons[i].interactable = true;
            }
        }

        private void Visit(int row)
        {
            if (row >= _owners.Count) return;
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            foreach (Button button in rowButtons) button.interactable = false;
            body.text = "Sailing over...";
            _visits.StartVisit(_owners[row]);
        }

        private void OnVisitFailed(VisitFailed evt)
        {
            body.text = "That island could not be loaded. Try another one.";
            foreach (Button button in rowButtons) button.interactable = true;
        }

        private string TierName(int tier)
        {
            TierDefinition definition = _database.GetTier(tier);
            return definition != null ? definition.displayName : $"Tier {tier}";
        }
    }
}
