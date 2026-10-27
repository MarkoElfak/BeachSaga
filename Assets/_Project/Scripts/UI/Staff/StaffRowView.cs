using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>One worker in the staff list: portrait, name and details, Move and Dismiss.</summary>
    public sealed class StaffRowView : MonoBehaviour
    {
        [SerializeField] private RawImage portrait = null;
        [SerializeField] private TMP_Text label = null;
        [SerializeField] private Button moveButton = null;
        [SerializeField] private Button dismissButton = null;
        [SerializeField] private TMP_Text dismissLabel = null;

        public string Text => label.text;
        public string DismissText => dismissLabel.text;
        public Button MoveButton => moveButton;
        public Button DismissButton => dismissButton;

        public void Bind(int index, Action<int> onMove, Action<int> onDismiss)
        {
            moveButton.onClick.AddListener(() => onMove(index));
            dismissButton.onClick.AddListener(() => onDismiss(index));
        }

        public void Show(Texture picture, string text, string dismiss)
        {
            portrait.texture = picture;
            label.text = text;
            dismissLabel.text = dismiss;
        }

        private void OnDestroy()
        {
            moveButton.onClick.RemoveAllListeners();
            dismissButton.onClick.RemoveAllListeners();
        }
    }
}
