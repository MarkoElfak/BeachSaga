namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Decides when a phone can't keep up with the picture: over each window of play, if enough frames took longer
    /// than the target frame rate allows, it asks for one step down. Very long frames (a pause, a scene load) are
    /// left out, so only steady slowness counts.
    /// </summary>
    public sealed class FrameRateGovernor
    {
        private const float IgnoreFramesAbove = 0.25f;

        private readonly float _slowFrameSeconds;
        private readonly float _windowSeconds;
        private readonly float _slowShare;
        private float _elapsed;
        private int _frames;
        private int _slowFrames;

        /// <param name="slowFactor">A frame is slow when it takes this many times the target frame's time.</param>
        /// <param name="slowShare">The share of slow frames in a window that asks for a step down.</param>
        public FrameRateGovernor(int targetFrameRate, float slowFactor, float windowSeconds, float slowShare)
        {
            _slowFrameSeconds = slowFactor / (targetFrameRate > 0 ? targetFrameRate : 60);
            _windowSeconds = windowSeconds;
            _slowShare = slowShare;
        }

        /// <summary>Counts one frame; true when a window just ended with too many slow frames.</summary>
        public bool Sample(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime > IgnoreFramesAbove) return false;
            _elapsed += unscaledDeltaTime;
            _frames++;
            if (unscaledDeltaTime > _slowFrameSeconds) _slowFrames++;
            if (_elapsed < _windowSeconds) return false;

            bool slow = _slowFrames >= _frames * _slowShare;
            _elapsed = 0f;
            _frames = 0;
            _slowFrames = 0;
            return slow;
        }
    }
}
