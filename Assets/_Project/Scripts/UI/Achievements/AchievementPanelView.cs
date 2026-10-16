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
    /// HUD trophy button (badge = rewards waiting) and a scrollable list of achievements. Claimable ones
    /// are listed first and finished ones last; rows are created once and refreshed on events.
    /// </summary>
    public sealed class AchievementPanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton = null;
        [SerializeField] private GameObject badge = null;
        [SerializeField] private TMP_Text badgeLabel = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private ScrollRect scroll = null;
        [SerializeField] private RectTransform content = null;
        [SerializeField] private AchievementRowView rowPrefab = null;
        [SerializeField] private TMP_Text summary = null;

        private readonly List<AchievementRowView> _rows = new List<AchievementRowView>();
        private AchievementService _achievements;
        private TutorialService _tutorial;

        public bool IsOpen => root.activeSelf;
        public AchievementRowView Row(int index) => _rows[index];

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _achievements))
            {
                openButton.gameObject.SetActive(false);
                return;
            }

            _tutorial = ServiceLocator.Get<TutorialService>();
            for (int i = 0; i < _achievements.Book.Count; i++)
            {
                AchievementRowView row = Instantiate(rowPrefab, content);
                row.name = $"Row_{_achievements.Book.Get(i).id}";
                row.Bind(i, Claim);
                _rows.Add(row);
            }

            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            EventBus.Subscribe<AchievementProgressed>(OnProgressed);
            EventBus.Subscribe<AchievementCompleted>(OnCompleted);
            EventBus.Subscribe<AchievementClaimed>(OnClaimed);
            EventBus.Subscribe<TutorialStepChanged>(OnTutorialStep);
            RefreshButton();
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            EventBus.Unsubscribe<AchievementProgressed>(OnProgressed);
            EventBus.Unsubscribe<AchievementCompleted>(OnCompleted);
            EventBus.Unsubscribe<AchievementClaimed>(OnClaimed);
            EventBus.Unsubscribe<TutorialStepChanged>(OnTutorialStep);
        }

        public void Open()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            root.SetActive(true);
            RefreshAll();
            scroll.verticalNormalizedPosition = 1f;
        }

        public void Close() => root.SetActive(false);

        private void Claim(int index) => _achievements.Claim(index);

        private void RefreshButton()
        {
            bool available = !_tutorial.IsActive;
            if (openButton.gameObject.activeSelf != available) openButton.gameObject.SetActive(available);
            int waiting = _achievements.Book.ClaimableCount;
            bool show = available && waiting > 0;
            if (badge.activeSelf != show) badge.SetActive(show);
            if (show) badgeLabel.text = waiting.ToString();
        }

        // Claimable first, then in progress, then finished; definition order inside each group.
        private void RefreshAll()
        {
            int sibling = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if ((int)StateOf(i) != Order(pass)) continue;
                    RefreshRow(i);
                    _rows[i].transform.SetSiblingIndex(sibling++);
                }
            }

            AchievementBook book = _achievements.Book;
            summary.text = $"{book.CompletedCount} of {book.Count} done";
        }

        private static int Order(int pass) => pass == 0 ? (int)AchievementRowView.State.Claimable
            : pass == 1 ? (int)AchievementRowView.State.InProgress : (int)AchievementRowView.State.Claimed;

        private AchievementRowView.State StateOf(int index)
        {
            AchievementBook book = _achievements.Book;
            if (book.IsClaimed(index)) return AchievementRowView.State.Claimed;
            return book.IsComplete(index) ? AchievementRowView.State.Claimable : AchievementRowView.State.InProgress;
        }

        private void RefreshRow(int index)
        {
            AchievementBook book = _achievements.Book;
            AchievementDefinition definition = book.Get(index);
            string progress = $"{NumberFormatter.Format(book.Progress(index))} / {NumberFormatter.Format(definition.target)}";
            _rows[index].Show(definition.title, book.Describe(index), book.Fraction(index), progress, RewardText(definition), StateOf(index));
        }

        private static string RewardText(AchievementDefinition definition)
        {
            string coins = "+" + NumberFormatter.Format(definition.rewardCoins);
            return definition.rewardBoostMinutes > 0f ? $"{coins}\n+{definition.rewardBoostMinutes:0}m 2x" : coins;
        }

        private void OnProgressed(AchievementProgressed evt)
        {
            if (IsOpen) RefreshRow(evt.Index);
        }

        private void OnCompleted(AchievementCompleted evt)
        {
            RefreshButton();
            if (IsOpen) RefreshRow(evt.Index);
        }

        private void OnClaimed(AchievementClaimed evt)
        {
            RefreshButton();
            if (IsOpen) RefreshAll();
        }

        private void OnTutorialStep(TutorialStepChanged evt) => RefreshButton();
    }
}
