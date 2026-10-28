using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The picked country's bonus for the whole beach: Filoxenia (visitors stay longer), Fiesta (more coins at night
    /// and during events) or Fatto a mano (higher prices). Sales and visitors read the multipliers below; nothing is
    /// saved. Announces when a Fiesta starts and ends.
    /// </summary>
    public sealed class CultureAbilities : ITickable
    {
        private readonly CultureService _cultures;
        private readonly DayClock _clock;
        private readonly BeachEventService _events;
        private bool _fiesta;

        public CultureAbilities(CultureService cultures, DayClock clock = null, BeachEventService events = null)
        {
            _cultures = cultures;
            _clock = clock;
            _events = events;
            _fiesta = IsFiesta;
        }

        /// <summary>The picked country's ability; null before a culture is picked or when it has none.</summary>
        public CultureAbility Current
        {
            get
            {
                CultureDefinition culture = _cultures.Current;
                CultureAbility ability = culture != null ? culture.ability : null;
                return ability != null && ability.kind != CultureAbilityKind.None ? ability : null;
            }
        }

        public bool Has(CultureAbilityKind kind) => Current != null && Current.kind == kind;

        /// <summary>True while night-and-events earnings are raised (it is night, or an event runs).</summary>
        public bool IsFiesta =>
            Has(CultureAbilityKind.NightAndEvents) && ((_clock != null && _clock.IsNight) || (_events != null && _events.IsActive));

        /// <summary>Scales every sale right now.</summary>
        public float PriceMultiplier
        {
            get
            {
                if (Has(CultureAbilityKind.HigherPrices) || IsFiesta) return 1f + Current.amount;
                return 1f;
            }
        }

        /// <summary>Scales how long visitors relax on the beach.</summary>
        public float StayMultiplier => Has(CultureAbilityKind.LongerStays) ? 1f + Current.amount : 1f;

        /// <summary>The price bonus averaged over a day (offline earnings): a Fiesta only counts for the night.</summary>
        public float AveragePriceMultiplier
        {
            get
            {
                if (Has(CultureAbilityKind.HigherPrices)) return 1f + Current.amount;
                if (Has(CultureAbilityKind.NightAndEvents)) return 1f + Current.amount * DayClock.NightShare;
                return 1f;
            }
        }

        public void Tick(float deltaTime)
        {
            bool fiesta = IsFiesta;
            if (fiesta == _fiesta) return;
            _fiesta = fiesta;
            EventBus.Publish(new FiestaChanged(fiesta));
        }
    }
}
