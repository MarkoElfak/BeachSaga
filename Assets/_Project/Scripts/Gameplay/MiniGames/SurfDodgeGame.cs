using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Surf dodge: the board rides at the bottom of the sea in one of 'choices' lanes; every 'pace' seconds a row
    /// of rocks and buoys comes down at 'speed' screen heights per second, always with at least one lane open.
    /// Each row passed scores; hitting one costs a life (then a short moment of safety).
    /// Uses target, lives, speed, pace, choices (2 to MaxLanes) and timeLimit. Positions run 0 (bottom) to 1 (top).
    /// </summary>
    public sealed class SurfDodgeGame : MiniGame
    {
        public const int MaxLanes = 5;
        public const int MaxRows = 12;
        public const float BoardY = 0.14f;
        public const float HitRange = 0.05f;
        private const float SafeSeconds = 0.9f;
        private const float AutoStepGap = 0.12f;

        private readonly float[] _rowY = new float[MaxRows];
        private readonly int[] _rowMask = new int[MaxRows];
        private readonly byte[] _rowState = new byte[MaxRows]; // 0 free, 1 coming, 2 passed or hit
        private float _nextRow;
        private float _autoCooldown;

        public override MiniGameKind Kind => MiniGameKind.SurfDodge;
        public int Lanes => Level.choices < 2 ? 2 : Level.choices > MaxLanes ? MaxLanes : Level.choices;
        public int BoardLane { get; private set; }
        public float SafeLeft { get; private set; }

        public bool IsRowActive(int row) => _rowState[row] != 0;
        public float RowY(int row) => _rowY[row];
        public bool Blocks(int row, int lane) => (_rowMask[row] & (1 << lane)) != 0;

        public void Steer(int direction)
        {
            if (!IsPlaying) return;
            int lane = BoardLane + (direction < 0 ? -1 : 1);
            if (lane >= 0 && lane < Lanes) BoardLane = lane;
        }

        // Head for the nearest open lane of the next row coming down, one lane at a time.
        public override void AutoPlay()
        {
            if (!IsPlaying || _autoCooldown > 0f) return;
            int next = -1;
            for (int row = 0; row < MaxRows; row++)
            {
                if (_rowState[row] == 1 && (next < 0 || _rowY[row] < _rowY[next])) next = row;
            }

            if (next < 0 || !Blocks(next, BoardLane)) return;
            int target = -1;
            for (int lane = 0; lane < Lanes; lane++)
            {
                if (!Blocks(next, lane) && (target < 0 || System.Math.Abs(lane - BoardLane) < System.Math.Abs(target - BoardLane))) target = lane;
            }

            Steer(target - BoardLane);
            _autoCooldown = AutoStepGap;
        }

        protected override void OnBegin()
        {
            for (int row = 0; row < MaxRows; row++) _rowState[row] = 0;
            BoardLane = Lanes / 2;
            SafeLeft = 0f;
            _nextRow = 0.3f;
            _autoCooldown = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            _autoCooldown -= deltaTime;
            SafeLeft -= deltaTime;
            for (int row = 0; row < MaxRows && IsPlaying; row++)
            {
                if (_rowState[row] == 0) continue;
                _rowY[row] -= Level.speed * deltaTime;
                if (_rowState[row] == 1) Resolve(row);
                if (_rowY[row] < -0.15f) _rowState[row] = 0;
            }

            _nextRow -= deltaTime;
            if (_nextRow > 0f || !IsPlaying) return;
            _nextRow += Level.pace;
            SpawnRow();
        }

        // Crash while the row is level with the board in a blocked lane; otherwise it scores once it is behind.
        private void Resolve(int row)
        {
            float y = _rowY[row];
            if (y <= BoardY + HitRange && y >= BoardY - HitRange && Blocks(row, BoardLane) && SafeLeft <= 0f)
            {
                _rowState[row] = 2;
                SafeLeft = SafeSeconds;
                Miss();
            }
            else if (y < BoardY - HitRange)
            {
                _rowState[row] = 2;
                Hit();
            }
        }

        private void SpawnRow()
        {
            int slot = -1;
            for (int row = 0; row < MaxRows && slot < 0; row++) if (_rowState[row] == 0) slot = row;
            if (slot < 0) return;

            int blocked = 1 + NextInt(Lanes - 1); // 1 .. lanes-1, so a lane is always open
            int mask = 0;
            while (blocked > 0)
            {
                int lane = NextInt(Lanes);
                if ((mask & (1 << lane)) != 0) continue;
                mask |= 1 << lane;
                blocked--;
            }

            _rowMask[slot] = mask;
            _rowY[slot] = 1.05f;
            _rowState[slot] = 1;
        }
    }
}
