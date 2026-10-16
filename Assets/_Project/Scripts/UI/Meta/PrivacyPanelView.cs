using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The privacy question. Shown by itself on first launch, where it must be answered (no close button), and
    /// from Settings, where it shows the current choice and can be closed. The policy link only appears once a
    /// privacy policy URL is set in MonetizationConfig.
    /// </summary>
    public sealed class PrivacyPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private RectTransform window = null;
        [Tooltip("Height taken by the policy link row; the window shrinks by this much when there is no policy URL.")]
        [SerializeField] private float policyRowHeight = 150f;
        [SerializeField] private TMP_Text status = null;
        [SerializeField] private Button acceptButton = null;
        [SerializeField] private Button necessaryButton = null;
        [SerializeField] private Button policyButton = null;
        [SerializeField] private Button closeButton = null;

        private PrivacyService _privacy;
        private string _policyUrl;

        public bool IsOpen => root.activeSelf;
        public bool CanClose => closeButton.gameObject.activeSelf;
        public string StatusText => status.text;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _privacy)) return;
            _policyUrl = ServiceLocator.Get<GameDatabase>().monetization.privacyPolicyUrl;
            bool hasPolicy = !string.IsNullOrEmpty(_policyUrl);
            policyButton.gameObject.SetActive(hasPolicy);
            if (!hasPolicy) window.sizeDelta -= new Vector2(0f, policyRowHeight);
            acceptButton.onClick.AddListener(Accept);
            necessaryButton.onClick.AddListener(OnlyNecessary);
            policyButton.onClick.AddListener(OpenPolicy);
            closeButton.onClick.AddListener(Close);
            if (!_privacy.HasAnswered) Open();
        }

        private void OnDestroy()
        {
            acceptButton.onClick.RemoveListener(Accept);
            necessaryButton.onClick.RemoveListener(OnlyNecessary);
            policyButton.onClick.RemoveListener(OpenPolicy);
            closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            closeButton.gameObject.SetActive(_privacy.HasAnswered);
            status.text = StatusFor(_privacy.Choice);
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);

        private void Accept() => Answer(PrivacyChoice.Accepted);
        private void OnlyNecessary() => Answer(PrivacyChoice.Declined);
        private void OpenPolicy() => Application.OpenURL(_policyUrl);

        private void Answer(PrivacyChoice choice)
        {
            _privacy.Answer(choice);
            Close();
        }

        private static string StatusFor(PrivacyChoice choice)
        {
            switch (choice)
            {
                case PrivacyChoice.Accepted: return "Your choice: statistics and personalised ads are on.";
                case PrivacyChoice.Declined: return "Your choice: only what the game needs to run.";
                default: return "You can change this any time in Settings.";
            }
        }
    }
}
