using BeachTycoon.Core;
using BeachTycoon.Data;

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

        public bool SoundOn
        {
            get => _settings.soundOn;
            set => Set(ref _settings.soundOn, value);
        }

        public bool MusicOn
        {
            get => _settings.musicOn;
            set => Set(ref _settings.musicOn, value);
        }

        private static void Set(ref bool field, bool value)
        {
            if (field == value) return;
            field = value;
            EventBus.Publish(new SettingsChanged());
        }
    }
}
