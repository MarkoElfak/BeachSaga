using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>Phone vibration. Swap in a native plugin for richer iOS taps without touching callers.</summary>
    public interface IHapticsService
    {
        /// <summary>Pulses played so far (tests, debug).</summary>
        int PulseCount { get; }
        HapticStrength LastStrength { get; }

        void Play(HapticStrength strength);
    }
}
