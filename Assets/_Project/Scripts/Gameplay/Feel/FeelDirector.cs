using System;
using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Turns big moments into screen feedback: a finished building shakes the camera and throws confetti; a
    /// passed mini-game level, a trophy and a new neighbour throw confetti. Amounts come from PresentationConfig.
    /// </summary>
    public sealed class FeelDirector : IDisposable
    {
        private readonly PresentationConfig _config;

        public FeelDirector(PresentationConfig config)
        {
            _config = config;
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<MiniGameLevelPassed>(OnMiniGameLevelPassed);
            EventBus.Subscribe<AchievementCompleted>(OnAchievementCompleted);
            EventBus.Subscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<MiniGameLevelPassed>(OnMiniGameLevelPassed);
            EventBus.Unsubscribe<AchievementCompleted>(OnAchievementCompleted);
            EventBus.Unsubscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
        }

        private void OnTierUpgraded(TierUpgraded evt)
        {
            EventBus.Publish(new CameraShakeRequested(1f));
            Confetti(_config.tierConfetti);
        }

        private void OnMiniGameLevelPassed(MiniGameLevelPassed evt) => Confetti(_config.levelConfetti);
        private void OnAchievementCompleted(AchievementCompleted evt) => Confetti(_config.trophyConfetti);
        private void OnNeighbourMovedIn(NeighbourMovedIn evt) => Confetti(_config.neighbourConfetti);

        private static void Confetti(int pieces)
        {
            if (pieces > 0) EventBus.Publish(new ConfettiRequested(pieces));
        }
    }
}
