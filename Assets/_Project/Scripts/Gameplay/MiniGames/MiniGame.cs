using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    public enum MiniGameState
    {
        Idle = 0,
        Playing = 1,
        Won = 2,
        Lost = 3,
    }

    /// <summary>
    /// Shared rules of every mini-game: reach the level's target before running out of lives (or time). Games are
    /// plain C# ticked by their view, seeded so a run can be replayed, and never allocate while playing.
    /// AutoPlay makes one good, human-paced move; tests use it to prove every level can be passed.
    /// </summary>
    public abstract class MiniGame
    {
        private System.Random _random = new System.Random(0);

        public MiniGameState State { get; private set; }
        public MiniGameLevel Level { get; private set; }
        public int Score { get; private set; }
        public int Mistakes { get; private set; }
        public float Elapsed { get; private set; }

        public bool IsOver => State == MiniGameState.Won || State == MiniGameState.Lost;
        public int Target => Level.target < 1 ? 1 : Level.target;
        public int LivesLeft => Level.lives - Mistakes < 0 ? 0 : Level.lives - Mistakes;
        public bool HasTimeLimit => Level.timeLimit > 0f;
        public float TimeLeft => HasTimeLimit && Level.timeLimit > Elapsed ? Level.timeLimit - Elapsed : 0f;

        public abstract MiniGameKind Kind { get; }

        public void Begin(MiniGameLevel level, int seed)
        {
            _random = new System.Random(seed);
            Level = level;
            Score = 0;
            Mistakes = 0;
            Elapsed = 0f;
            State = MiniGameState.Playing;
            OnBegin();
        }

        public void Tick(float deltaTime)
        {
            if (State != MiniGameState.Playing) return;
            Elapsed += deltaTime;
            OnTick(deltaTime);
            if (State == MiniGameState.Playing && HasTimeLimit && Elapsed >= Level.timeLimit) State = MiniGameState.Lost;
        }

        /// <summary>One good move for the current moment (or nothing while waiting), with a human reaction delay.</summary>
        public abstract void AutoPlay();

        protected abstract void OnBegin();
        protected abstract void OnTick(float deltaTime);

        protected bool IsPlaying => State == MiniGameState.Playing;

        protected void Hit()
        {
            if (!IsPlaying) return;
            Score++;
            if (Score >= Target) State = MiniGameState.Won;
        }

        protected void Miss()
        {
            if (!IsPlaying) return;
            Mistakes++;
            if (Mistakes >= Level.lives) State = MiniGameState.Lost;
        }

        protected int NextInt(int maxExclusive) => _random.Next(maxExclusive);
        protected float NextFloat() => (float)_random.NextDouble();

        /// <summary>A fresh game of this kind.</summary>
        public static MiniGame Create(MiniGameKind kind)
        {
            switch (kind)
            {
                case MiniGameKind.TowelRun: return new TowelRunGame();
                case MiniGameKind.PerfectPour: return new PerfectPourGame();
                case MiniGameKind.SurfDodge: return new SurfDodgeGame();
                case MiniGameKind.SkyBuilder: return new SkyBuilderGame();
                default: return new DonutRushGame();
            }
        }
    }
}
