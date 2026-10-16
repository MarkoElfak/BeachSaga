using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Rewarded "2x income for 10 minutes". Counts down in play time only and persists in the save,
    /// so closing the app pauses the boost instead of wasting it.
    /// </summary>
    public sealed class IncomeBoost : ITickable
    {
        private const float SecondsPerMinute = 60f;

        private readonly MonetizationConfig _config;
        private readonly MetaModel _meta;

        public IncomeBoost(MonetizationConfig config, MetaModel meta)
        {
            _config = config;
            _meta = meta;
        }

        public bool IsActive => _meta.incomeBoostSeconds > 0f;
        public float RemainingSeconds => _meta.incomeBoostSeconds;
        public float Multiplier => IsActive ? _config.incomeBoostMultiplier : 1f;

        /// <summary>Starts (or refreshes) the boost at its full duration.</summary>
        public void Activate()
        {
            _meta.incomeBoostSeconds = _config.incomeBoostMinutes * SecondsPerMinute;
            EventBus.Publish(new IncomeBoostChanged(true));
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            _meta.incomeBoostSeconds -= deltaTime;
            if (_meta.incomeBoostSeconds <= 0f)
            {
                _meta.incomeBoostSeconds = 0f;
                EventBus.Publish(new IncomeBoostChanged(false));
            }
        }
    }
}
