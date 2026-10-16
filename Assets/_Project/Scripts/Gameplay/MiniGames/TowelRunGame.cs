using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Towel run: every 'pace' seconds a visitor lies down on a free sunbed; tap them before 'patience' runs out to
    /// hand over a towel. A visitor who gives up costs a life; tapping an empty bed does nothing.
    /// Uses target, lives, pace, patience, choices (sunbeds, up to MaxBeds) and timeLimit.
    /// </summary>
    public sealed class TowelRunGame : MiniGame
    {
        public const int MaxBeds = 9;
        private const float AutoReaction = 0.35f;
        private const float AutoTapGap = 0.2f;

        private readonly float[] _waitLeft = new float[MaxBeds];
        private readonly float[] _waited = new float[MaxBeds];
        private float _nextArrival;
        private float _autoCooldown;

        public override MiniGameKind Kind => MiniGameKind.TowelRun;
        public int Beds => Level.choices < 1 ? 1 : Level.choices > MaxBeds ? MaxBeds : Level.choices;

        public bool IsWaiting(int bed) => _waitLeft[bed] > 0f;
        public float WaitFraction(int bed) => Level.patience > 0f ? _waitLeft[bed] / Level.patience : 0f;

        public void Tap(int bed)
        {
            if (!IsPlaying || bed < 0 || bed >= Beds || !IsWaiting(bed)) return;
            _waitLeft[bed] = 0f;
            Hit();
        }

        public override void AutoPlay()
        {
            if (!IsPlaying || _autoCooldown > 0f) return;
            int best = -1;
            for (int bed = 0; bed < Beds; bed++)
            {
                if (IsWaiting(bed) && _waited[bed] >= AutoReaction && (best < 0 || _waitLeft[bed] < _waitLeft[best])) best = bed;
            }

            if (best < 0) return;
            Tap(best);
            _autoCooldown = AutoTapGap;
        }

        protected override void OnBegin()
        {
            for (int bed = 0; bed < MaxBeds; bed++) _waitLeft[bed] = 0f;
            _nextArrival = 0.4f;
            _autoCooldown = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            _autoCooldown -= deltaTime;
            for (int bed = 0; bed < Beds && IsPlaying; bed++)
            {
                if (!IsWaiting(bed)) continue;
                _waited[bed] += deltaTime;
                _waitLeft[bed] -= deltaTime;
                if (_waitLeft[bed] > 0f) continue;
                _waitLeft[bed] = 0f;
                Miss();
            }

            _nextArrival -= deltaTime;
            if (_nextArrival > 0f || !IsPlaying) return;
            _nextArrival += Level.pace;
            Arrive();
        }

        // A visitor picks one of the free beds at random (none free: they walk on).
        private void Arrive()
        {
            int free = 0;
            for (int bed = 0; bed < Beds; bed++) if (!IsWaiting(bed)) free++;
            if (free == 0) return;

            int pick = NextInt(free);
            for (int bed = 0; bed < Beds; bed++)
            {
                if (IsWaiting(bed) || pick-- > 0) continue;
                _waitLeft[bed] = Level.patience;
                _waited[bed] = 0f;
                return;
            }
        }
    }
}
