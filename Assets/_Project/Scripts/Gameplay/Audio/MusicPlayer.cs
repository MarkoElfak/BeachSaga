using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Background music: the day loop and the night loop play together on two sources and crossfade over
    /// SoundLibrary.musicFadeSeconds as the DayClock passes into and out of night. Volume follows the setting.
    /// </summary>
    public sealed class MusicPlayer : ITickable, IDisposable
    {
        private readonly SoundLibrary _library;
        private readonly SettingsService _settings;
        private readonly DayClock _clock;
        private readonly AudioSource _day;
        private readonly AudioSource _night;
        private float _nightMix;

        public MusicPlayer(SoundLibrary library, SettingsService settings, DayClock clock, GameObject host)
        {
            _library = library;
            _settings = settings;
            _clock = clock;
            _day = CreateSource(host, library != null ? library.music : null);
            _night = CreateSource(host, library != null && library.nightMusic != null ? library.nightMusic : _day.clip);
            _nightMix = IsNight ? 1f : 0f;
            EventBus.Subscribe<SettingsChanged>(OnSettingsChanged);
            Apply();
        }

        /// <summary>0 = only the day loop is heard, 1 = only the night loop.</summary>
        public float NightMix => _nightMix;

        public float DayVolume => _day.volume;
        public float NightVolume => _night.volume;
        public bool IsPlaying => _day.isPlaying || _night.isPlaying;

        private bool IsNight => _clock != null && _clock.IsNight;
        private float FullVolume => _library != null ? _library.musicVolume * _settings.MusicVolume : 0f;

        public void Tick(float deltaTime)
        {
            float target = IsNight ? 1f : 0f;
            if (Mathf.Approximately(_nightMix, target)) return;
            float step = deltaTime / Mathf.Max(0.1f, _library != null ? _library.musicFadeSeconds : 1f);
            _nightMix = Mathf.MoveTowards(_nightMix, target, step);
            Apply();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SettingsChanged>(OnSettingsChanged);
            _day.Stop();
            _night.Stop();
        }

        private void OnSettingsChanged(SettingsChanged evt) => Apply();

        // Equal-power crossfade keeps the loudness steady halfway through.
        private void Apply()
        {
            float volume = FullVolume;
            bool on = volume > 0f && _day.clip != null;
            _day.volume = volume * Mathf.Cos(_nightMix * Mathf.PI * 0.5f);
            _night.volume = volume * Mathf.Sin(_nightMix * Mathf.PI * 0.5f);
            if (!on)
            {
                _day.Stop();
                _night.Stop();
                return;
            }

            if (!_day.isPlaying) _day.Play();
            if (!_night.isPlaying) _night.Play();
        }

        private static AudioSource CreateSource(GameObject host, AudioClip clip)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = clip;
            return source;
        }
    }
}
