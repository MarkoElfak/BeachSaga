using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Player settings stored in the save. Changing one publishes SettingsChanged.</summary>
    public sealed class SettingsService
    {
        private readonly SettingsModel _settings;

        public SettingsService(SettingsModel settings)
        {
            _settings = settings;
        }

        /// <summary>Sound effects volume, 0 (off) to 1.</summary>
        public float SoundVolume
        {
            get => _settings.soundVolume;
            set => Set(ref _settings.soundVolume, value);
        }

        /// <summary>Music volume, 0 (off) to 1.</summary>
        public float MusicVolume
        {
            get => _settings.musicVolume;
            set => Set(ref _settings.musicVolume, value);
        }

        public bool VibrationOn
        {
            get => _settings.vibrationOn;
            set
            {
                if (_settings.vibrationOn == value) return;
                _settings.vibrationOn = value;
                EventBus.Publish(new SettingsChanged());
            }
        }

        /// <summary>Off is volume 0; switching on restores full volume.</summary>
        public bool SoundOn
        {
            get => SoundVolume > 0f;
            set => SoundVolume = value ? 1f : 0f;
        }

        public bool MusicOn
        {
            get => MusicVolume > 0f;
            set => MusicVolume = value ? 1f : 0f;
        }

        private static void Set(ref float field, float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, value)) return;
            field = value;
            EventBus.Publish(new SettingsChanged());
        }
    }
}
