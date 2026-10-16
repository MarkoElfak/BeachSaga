namespace BeachTycoon.Core
{
    /// <summary>Asks the world camera for a short shake (1 = full CameraConfig.shakeAmplitude).</summary>
    public readonly struct CameraShakeRequested
    {
        public readonly float Strength;

        public CameraShakeRequested(float strength)
        {
            Strength = strength;
        }
    }

    /// <summary>Asks the HUD for a burst of confetti.</summary>
    public readonly struct ConfettiRequested
    {
        public readonly int Pieces;

        public ConfettiRequested(int pieces)
        {
            Pieces = pieces;
        }
    }
}
