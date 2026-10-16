using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Settings: sound and music volume sliders, vibration on/off, plus the (mock) Remove Ads purchase.</summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Slider soundSlider = null;
        [SerializeField] private TMP_Text soundLabel = null;
        [SerializeField] private Slider musicSlider = null;
        [SerializeField] private TMP_Text musicLabel = null;
        [SerializeField] private Button vibrationButton = null;
        [SerializeField] private TMP_Text vibrationLabel = null;
        [SerializeField] private Button removeAdsButton = null;
        [SerializeField] private TMP_Text removeAdsLabel = null;
        [SerializeField] private Button closeButton = null;

        private SettingsService _settings;
        private IIapService _iap;
        private string _removeAdsId;

        public bool IsOpen => root.activeSelf;
        public string SoundText => soundLabel.text;
        public string VibrationText => vibrationLabel.text;

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
            soundSlider.onValueChanged.AddListener(SetSound);
            musicSlider.onValueChanged.AddListener(SetMusic);
            vibrationButton.onClick.AddListener(ToggleVibration);
            removeAdsButton.onClick.AddListener(BuyRemoveAds);
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            soundSlider.onValueChanged.RemoveListener(SetSound);
            musicSlider.onValueChanged.RemoveListener(SetMusic);
            vibrationButton.onClick.RemoveListener(ToggleVibration);
            removeAdsButton.onClick.RemoveListener(BuyRemoveAds);
        }

        public void Open()
        {
            root.SetActive(true);
            soundSlider.SetValueWithoutNotify(_settings.SoundVolume);
            musicSlider.SetValueWithoutNotify(_settings.MusicVolume);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        private void SetSound(float volume)
        {
            _settings.SoundVolume = volume;
            Refresh();
        }

        private void SetMusic(float volume)
        {
            _settings.MusicVolume = volume;
            Refresh();
        }

        private void ToggleVibration()
        {
            _settings.VibrationOn = !_settings.VibrationOn;
            if (_settings.VibrationOn && ServiceLocator.TryGet(out IHapticsService haptics)) haptics.Play(HapticStrength.Medium);
            Refresh();
        }

        private void BuyRemoveAds()
        {
            _iap.Purchase(_removeAdsId, success => Refresh());
        }

        // Runs on changes only (open, slider drag, taps), so its strings are not a per-frame cost.
        private void Refresh()
        {
            soundLabel.text = VolumeText("Sound", _settings.SoundVolume);
            musicLabel.text = VolumeText("Music", _settings.MusicVolume);
            vibrationLabel.text = _settings.VibrationOn ? "Vibration: ON" : "Vibration: OFF";
            bool owned = _iap.IsOwned(_removeAdsId);
            removeAdsLabel.text = owned ? "Ads removed - thank you!" : "Remove ads";
            removeAdsButton.interactable = !owned;
        }

        private static string VolumeText(string name, float volume) =>
            volume <= 0f ? name + ": OFF" : $"{name}: {Mathf.RoundToInt(volume * 100f)}%";
    }
}
