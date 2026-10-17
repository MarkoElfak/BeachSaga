using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's village: founded once on the start screen with a name and a culture. The name can be changed
    /// later in Settings; the culture is kept for the whole game.
    /// </summary>
    public sealed class VillageService
    {
        private readonly VillageConfig _config;
        private readonly MetaModel _meta;
        private readonly IAnalyticsService _analytics;
        private readonly System.Random _random;
        private int _lastSuggestion = -1;

        public VillageService(VillageConfig config, MetaModel meta, IAnalyticsService analytics, int seed = 0)
        {
            _config = config;
            _meta = meta;
            _analytics = analytics;
            _random = new System.Random(seed != 0 ? seed : System.Environment.TickCount);
        }

        public string Name => _meta.villageName;
        public string CultureId => _meta.cultureId;
        public bool IsFounded => !string.IsNullOrEmpty(_meta.villageName);
        public int MaxNameLength => _config.maxNameLength;

        /// <summary>Null when the name can be used (clean holds it tidied up), else a message for the player.</summary>
        public string Check(string raw, out string clean) =>
            VillageNames.Check(raw, _config.minNameLength, _config.maxNameLength, out clean);

        /// <summary>Names the village and, if none was picked before, sets its culture for good.</summary>
        public bool TryFound(string raw, string cultureId, out string error)
        {
            error = Check(raw, out string clean);
            if (error != null) return false;

            bool first = !IsFounded;
            if (string.IsNullOrEmpty(_meta.cultureId)) _meta.cultureId = cultureId ?? string.Empty;
            SetName(clean);
            if (first) _analytics.LogEvent(AnalyticsEvents.VillageFounded, "culture", _meta.cultureId);
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

        /// <summary>A name idea from the config, never the same twice in a row.</summary>
        public string SuggestName()
        {
            string[] names = _config.suggestedNames;
            if (names == null || names.Length == 0) return string.Empty;
            int pick = _random.Next(names.Length);
            if (pick == _lastSuggestion && names.Length > 1) pick = (pick + 1) % names.Length;
            _lastSuggestion = pick;
            return names[pick];
        }

        private void SetName(string clean)
        {
            if (clean == _meta.villageName) return;
            _meta.villageName = clean;
            EventBus.Publish(new VillageRenamed(clean));
        }
    }
}
