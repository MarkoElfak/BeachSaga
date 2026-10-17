using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's privacy choice, asked once on first launch and changeable in Settings. Analytics and
    /// personalised ads are only used after "Accept"; until the player answers, nothing is collected.
    /// </summary>
    public sealed class PrivacyService
    {
        private readonly SettingsModel _settings;

        public PrivacyService(SettingsModel settings)
        {
            _settings = settings;
        }

        public PrivacyChoice Choice => (PrivacyChoice)_settings.privacy;
        public bool HasAnswered => Choice != PrivacyChoice.NotAsked;
        public bool AnalyticsAllowed => Choice == PrivacyChoice.Accepted;
        public bool PersonalizedAds => Choice == PrivacyChoice.Accepted;

        /// <summary>Records the answer; services that collect data hear about it through PrivacyChanged.</summary>
        public void Answer(PrivacyChoice choice)
        {
            if (choice == PrivacyChoice.NotAsked || choice == Choice) return;
            _settings.privacy = (int)choice;
            EventBus.Publish(new PrivacyChanged(AnalyticsAllowed, PersonalizedAds));
        }
    }
}
