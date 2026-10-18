using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's village: founded once on the start screen with a name and a culture. The name can be changed
    /// later in Settings; the culture is kept for the whole game. Villages founded before cultures existed pick
    /// one once.
    /// </summary>
    public sealed class VillageService
    {
        private readonly VillageConfig _config;
        private readonly MetaModel _meta;
        private readonly IAnalyticsService _analytics;
        private readonly Func<string, bool> _isCulture;
        private readonly System.Random _random;
        private int _lastSuggestion = -1;

        /// <param name="isCulture">Whether a culture id exists; null accepts any non-empty id.</param>
        public VillageService(VillageConfig config, MetaModel meta, IAnalyticsService analytics, Func<string, bool> isCulture = null,
            int seed = 0)
        {
            _config = config;
            _meta = meta;
            _analytics = analytics;
            _isCulture = isCulture ?? (id => !string.IsNullOrEmpty(id));
            _random = new System.Random(seed != 0 ? seed : System.Environment.TickCount);
        }

        public string Name => _meta.villageName;
        public string CultureId => _meta.cultureId;
        public bool IsFounded => !string.IsNullOrEmpty(_meta.villageName);
        public bool HasCulture => !string.IsNullOrEmpty(_meta.cultureId);

        /// <summary>A village from before cultures: it still has to pick one.</summary>
        public bool NeedsCulture => IsFounded && !HasCulture;
        public int MaxNameLength => _config.maxNameLength;

        /// <summary>Null when the name can be used (clean holds it tidied up), else a message for the player.</summary>
        public string Check(string raw, out string clean) =>
            VillageNames.Check(raw, _config.minNameLength, _config.maxNameLength, out clean);

        /// <summary>Names the village and sets its culture for good. A culture picked before is kept.</summary>
        public bool TryFound(string raw, string cultureId, out string error)
        {
            error = Check(raw, out string clean);
            if (error == null && !HasCulture && !_isCulture(cultureId)) error = "Pick a culture.";
            if (error != null) return false;

            bool first = !IsFounded;
            SetName(clean);
            if (!HasCulture) SetCulture(cultureId);
            if (first) _analytics.LogEvent(AnalyticsEvents.VillageFounded, "culture", _meta.cultureId);
            return true;
        }

        /// <summary>For villages founded before cultures: picks one, once.</summary>
        public bool TryChooseCulture(string cultureId, out string error)
        {
            error = !IsFounded ? "Found the village first."
                : HasCulture ? "Your culture is already chosen."
                : !_isCulture(cultureId) ? "Pick a culture."
                : null;
            if (error != null) return false;
            SetCulture(cultureId);
            return true;
        }

        public bool TryRename(string raw, out string error)
        {
            if (!IsFounded)
            {
                error = "Found the village first.";
                return false;
            }

            error = Check(raw, out string clean);
            if (error != null) return false;
            SetName(clean);
            return true;
        }

        /// <summary>A name idea, never the same twice in a row: the culture's own ideas when it has some.</summary>
        public string SuggestName(string[] cultureNames = null)
        {
            string[] names = cultureNames != null && cultureNames.Length > 0 ? cultureNames : _config.suggestedNames;
            if (names == null || names.Length == 0) return string.Empty;
            int pick = _random.Next(names.Length);
            if (pick == _lastSuggestion && names.Length > 1) pick = (pick + 1) % names.Length;
            _lastSuggestion = pick;
            return names[pick];
        }

        private void SetCulture(string cultureId)
        {
            _meta.cultureId = cultureId;
            _analytics.LogEvent(AnalyticsEvents.CultureChosen, "culture", cultureId);
            EventBus.Publish(new CultureChosen(cultureId));
        }

        private void SetName(string clean)
        {
            if (clean == _meta.villageName) return;
            _meta.villageName = clean;
            EventBus.Publish(new VillageRenamed(clean));
        }
    }
}
