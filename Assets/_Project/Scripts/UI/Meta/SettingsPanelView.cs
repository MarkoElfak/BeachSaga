using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Settings: sound and music on/off, plus the (mock) Remove Ads purchase.</summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Button soundButton = null;
        [SerializeField] private TMP_Text soundLabel = null;
        [SerializeField] private Button musicButton = null;
        [SerializeField] private TMP_Text musicLabel = null;
        [SerializeField] private Button removeAdsButton = null;
        [SerializeField] private TMP_Text removeAdsLabel = null;
        [SerializeField] private Button closeButton = null;

        private SettingsService _settings;
        private IIapService _iap;
        private string _removeAdsId;

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _settings))
            {
                return;
            }

            _iap = ServiceLocator.Get<IIapService>();
            _removeAdsId = ServiceLocator.Get<GameDatabase>().monetization.removeAdsProductId;
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            soundButton.onClick.AddListener(ToggleSound);
            musicButton.onClick.AddListener(ToggleMusic);
            removeAdsButton.onClick.AddListener(BuyRemoveAds);
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            soundButton.onClick.RemoveListener(ToggleSound);
            musicButton.onClick.RemoveListener(ToggleMusic);
            removeAdsButton.onClick.RemoveListener(BuyRemoveAds);
        }

        public void Open()
        {
            root.SetActive(true);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        private void ToggleSound()
        {
            _settings.SoundOn = !_settings.SoundOn;
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            Refresh();
        }

        private void ToggleMusic()
        {
            _settings.MusicOn = !_settings.MusicOn;
            Refresh();
        }

        private void BuyRemoveAds()
        {
            _iap.Purchase(_removeAdsId, success => Refresh());
        }

        private void Refresh()
        {
            soundLabel.text = _settings.SoundOn ? "Sound: ON" : "Sound: OFF";
            musicLabel.text = _settings.MusicOn ? "Music: ON" : "Music: OFF";
            bool owned = _iap.IsOwned(_removeAdsId);
            removeAdsLabel.text = owned ? "Ads removed - thank you!" : "Remove ads";
            removeAdsButton.interactable = !owned;
        }
    }
}
