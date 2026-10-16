using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Perfect pour: hold to fill the glass ('speed' of the glass per second) and let go while the drink is inside
    /// the green band ('tolerance' of the glass high, at a random height). Letting go outside it or overflowing
    /// costs a life. After each glass there is a short pause to show the result.
    /// Uses target, lives, speed, tolerance and timeLimit.
    /// </summary>
    public sealed class PerfectPourGame : MiniGame
    {
        public const float RestSeconds = 0.6f;
        private const float BandLow = 0.4f;
        private const float BandHigh = 0.85f;

        public override MiniGameKind Kind => MiniGameKind.PerfectPour;
        public float Fill { get; private set; }
        public bool Pouring { get; private set; }
        public float BandMin { get; private set; }
        public float BandMax { get; private set; }
        public bool Resting => _restLeft > 0f;

        /// <summary>Whether the glass just judged was good (shown while resting).</summary>
        public bool LastGood { get; private set; }

        private float _restLeft;

        public void StartPour()
        {
            if (IsPlaying && !Pouring && !Resting && Fill <= 0f) Pouring = true;
        }

        public void StopPour()
        {
            if (!IsPlaying || !Pouring) return;
            Pouring = false;
            Judge(Fill >= BandMin && Fill <= BandMax);
        }

        public override void AutoPlay()
        {
            if (!IsPlaying || Resting) return;
            if (!Pouring && Fill <= 0f) StartPour();
            else if (Pouring && Fill >= (BandMin + BandMax) * 0.5f) StopPour();
        }

        protected override void OnBegin()
        {
            Pouring = false;
            _restLeft = 0f;
            NewGlass();
        }

        protected override void OnTick(float deltaTime)
        {
            if (Resting)
            {
                _restLeft -= deltaTime;
                if (!Resting) NewGlass();
                return;
            }

            if (!Pouring) return;
            Fill += Level.speed * deltaTime;
            if (Fill < 1f) return;
            Fill = 1f;
            Pouring = false;
            Judge(false); // overflow
        }

        private void Judge(bool good)
        {
            LastGood = good;
            _restLeft = RestSeconds;
            if (good) Hit();
            else Miss();
        }

        private void NewGlass()
        {
            Fill = 0f;
            float half = Level.tolerance * 0.5f;
            float centre = BandLow + half + NextFloat() * (BandHigh - BandLow - 2f * half > 0f ? BandHigh - BandLow - 2f * half : 0f);
            BandMin = centre - half;
            BandMax = centre + half;
        }
    }
}
