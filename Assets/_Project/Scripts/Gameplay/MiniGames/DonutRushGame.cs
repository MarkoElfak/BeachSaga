using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Donut rush: a customer asks for one icing; tap it before their patience runs out. A wrong icing costs a
    /// life (they keep waiting); running out of patience costs a life and the next customer steps up.
    /// Uses target, lives, patience, choices (icings, up to MaxChoices) and timeLimit.
    /// </summary>
    public sealed class DonutRushGame : MiniGame
    {
        public const int MaxChoices = 6;
        private const float AutoReaction = 0.45f;

        public override MiniGameKind Kind => MiniGameKind.DonutRush;
        public int Choices => Level.choices < 2 ? 2 : Level.choices > MaxChoices ? MaxChoices : Level.choices;
        public int Wanted { get; private set; }
        public float PatienceLeft { get; private set; }
        public float PatienceFraction => Level.patience > 0f ? PatienceLeft / Level.patience : 0f;

        /// <summary>Counts customers served or lost, so a view can tell a new customer arrived.</summary>
        public int Customer { get; private set; }

        private float _orderAge;

        public void Choose(int icing)
        {
            if (!IsPlaying) return;
            if (icing != Wanted)
            {
                Miss();
                return;
            }

            Hit();
            if (IsPlaying) NextCustomer();
        }

        public override void AutoPlay()
        {
            if (IsPlaying && _orderAge >= System.Math.Min(AutoReaction, Level.patience * 0.4f)) Choose(Wanted);
        }

        protected override void OnBegin()
        {
            Customer = 0;
            NextCustomer();
        }

        protected override void OnTick(float deltaTime)
        {
            _orderAge += deltaTime;
            PatienceLeft -= deltaTime;
            if (PatienceLeft > 0f) return;
            Miss();
            if (IsPlaying) NextCustomer();
        }

        // A different icing from the last one, so each new order is visibly new.
        private void NextCustomer()
        {
            if (Customer == 0)
            {
                Wanted = NextInt(Choices);
            }
            else
            {
                int icing = NextInt(Choices - 1);
                Wanted = icing >= Wanted ? icing + 1 : icing;
            }

            PatienceLeft = Level.patience;
            _orderAge = 0f;
            Customer++;
        }
    }
}
