using BeachTycoon.Core;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>Sound, music, vibration and screen feedback services.</summary>
    public sealed partial class GameBootstrap
    {
        private void AddFeedback(SettingsService settings, DayClock dayClock)
        {
            var haptics = new DeviceHapticsService();
            ServiceLocator.Register<IHapticsService>(haptics);
            Add(new AudioService(database.sounds, settings, gameObject, haptics));
            Add(new MusicPlayer(database.sounds, settings, dayClock, gameObject));
            Add(new FeelDirector(database.presentation));
        }
    }
}
