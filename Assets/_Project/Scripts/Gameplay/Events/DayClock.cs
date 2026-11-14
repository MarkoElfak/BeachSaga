using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Time of day in play time: the phase loops 0..1 over DayNightConfig.cycleMinutes and is kept in
    /// the save, so the day carries on after a restart. Views read Phase; nothing here touches the scene.
    /// </summary>
    public sealed class DayClock : ITickable
    {
        private const float SecondsPerMinute = 60f;
        private const float NightStart = 0.55f;
        private const float NightEnd = 0.95f;

        /// <summary>Share of the day that is night.</summary>
        public const float NightShare = NightEnd - NightStart;

        private readonly DayNightConfig _config;
        private readonly MetaModel _meta;

        public DayClock(DayNightConfig config, MetaModel meta)
        {
            _config = config;
            _meta = meta;
            _meta.dayPhase = Mathf.Repeat(_meta.dayPhase, 1f);
        }

        /// <summary>0 dawn, 0.25 midday, 0.5 sunset, 0.75 midnight.</summary>
        public float Phase => _meta.dayPhase;

        public bool IsNight => Phase >= NightStart && Phase < NightEnd;

        public float CycleSeconds => Mathf.Max(1f, _config.cycleMinutes * SecondsPerMinute);

        public void Tick(float deltaTime) => Advance(deltaTime);

        public void Advance(float seconds)
        {
            if (seconds > 0f) _meta.dayPhase = Mathf.Repeat(_meta.dayPhase + seconds / CycleSeconds, 1f);
        }

        public void SetPhase(float phase) => _meta.dayPhase = Mathf.Repeat(phase, 1f);
    }
}
