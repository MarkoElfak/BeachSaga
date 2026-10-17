using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>One achievement in the list: title, goal, progress bar, reward and a Claim button.</summary>
    public sealed class AchievementRowView : MonoBehaviour
    {
        public enum State
        {
            InProgress,
            Claimable,
            Claimed,
        }

        [SerializeField] private Image background = null;
        [SerializeField] private TMP_Text titleLabel = null;
        [SerializeField] private TMP_Text descriptionLabel = null;
        [SerializeField] private RectTransform progressFill = null;
        [SerializeField] private TMP_Text progressLabel = null;
        [SerializeField] private TMP_Text rewardLabel = null;
        [SerializeField] private Button claimButton = null;
        [SerializeField] private TMP_Text claimLabel = null;
        [SerializeField] private Color progressColor = new Color(0.93f, 0.96f, 1f);
        [SerializeField] private Color claimableColor = new Color(1f, 0.88f, 0.5f);
        [SerializeField] private Color claimedColor = new Color(0.72f, 0.9f, 0.7f);

        private Action<int> _onClaim;

        public int Index { get; private set; }
        public State Shown { get; private set; }

        public void Bind(int index, Action<int> onClaim)
        {
            Index = index;
            _onClaim = onClaim;
            claimButton.onClick.AddListener(OnClaim);
        }

        private void OnDestroy() => claimButton.onClick.RemoveListener(OnClaim);

        public void Show(string title, string description, float fraction, string progress, string reward, State state)
        {
            Shown = state;
            titleLabel.text = title;
            descriptionLabel.text = description;
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            progressLabel.text = progress;
            rewardLabel.text = reward;
            background.color = state == State.Claimable ? claimableColor : state == State.Claimed ? claimedColor : progressColor;
            claimButton.gameObject.SetActive(state != State.InProgress);
            claimButton.interactable = state == State.Claimable;
            claimLabel.text = state == State.Claimed ? "Done" : "Claim";
        }

        private void OnClaim() => _onClaim?.Invoke(Index);
    }
}
