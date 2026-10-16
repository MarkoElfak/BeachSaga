using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Sky builder: a new floor slides from side to side above the tower (-1 to 1, 'speed' full slides per
    /// second); tap to drop it. Within 'tolerance' of the middle it lands and the tower grows; otherwise it falls
    /// off and costs a life. A short pause shows each drop.
    /// Uses target, lives, speed, tolerance and timeLimit.
    /// </summary>
    public sealed class SkyBuilderGame : MiniGame
    {
        public const float RestSeconds = 0.45f;

        private int _direction = 1;
        private float _restLeft;

        public override MiniGameKind Kind => MiniGameKind.SkyBuilder;
        public float FloorX { get; private set; }
        public bool Resting => _restLeft > 0f;
        public float RestLeft => _restLeft > 0f ? _restLeft : 0f;
        public bool LastGood { get; private set; }

        /// <summary>Where the last floor was dropped (-1..1), for its landing or falling animation.</summary>
        public float LastX { get; private set; }

        public void Drop()
        {
            if (!IsPlaying || Resting) return;
            LastX = FloorX;
            LastGood = System.Math.Abs(FloorX) <= Level.tolerance;
            _restLeft = RestSeconds;
            if (LastGood) Hit();
            else Miss();
        }

        public override void AutoPlay()
        {
            if (IsPlaying && !Resting && System.Math.Abs(FloorX) <= Level.tolerance * 0.4f) Drop();
        }

        protected override void OnBegin()
        {
            _restLeft = 0f;
            NewFloor();
        }

        protected override void OnTick(float deltaTime)
        {
            if (Resting)
            {
                _restLeft -= deltaTime;
                if (!Resting) NewFloor();
                return;
            }

            FloorX += _direction * Level.speed * 2f * deltaTime;
            if (FloorX > 1f) { FloorX = 2f - FloorX; _direction = -1; }
            else if (FloorX < -1f) { FloorX = -2f - FloorX; _direction = 1; }
        }

        private void NewFloor()
        {
            _direction = NextInt(2) == 0 ? 1 : -1;
            FloorX = -_direction;
        }
    }
}
