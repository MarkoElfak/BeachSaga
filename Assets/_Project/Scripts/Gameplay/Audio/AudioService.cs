using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Lets UI or other code ask for a sound hook without referencing AudioService.</summary>
    public readonly struct SoundRequested
    {
        public readonly SoundId Id;

        public SoundRequested(SoundId id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// Sound and vibration hooks: maps gameplay events to SoundIds, plays the SoundLibrary's clip for each (with
    /// its volume, pitch variation and repeat limit, scaled by the sound volume setting) and pulses the phone as
    /// the slot says when vibration is on. A small round-robin pool of 2D AudioSources avoids allocations.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private readonly SoundLibrary _library;
        private readonly SettingsService _settings;
        private readonly IHapticsService _haptics;
        private readonly AudioSource[] _voices;
        private readonly float[] _lastPlayed;
        private readonly int[] _played;
        private int _nextVoice;

        public AudioService(SoundLibrary library, SettingsService settings, GameObject host, IHapticsService haptics = null)
        {
            _library = library;
            _settings = settings;
            _haptics = haptics;
            int voiceCount = library != null ? Mathf.Max(1, library.voices) : 1;
            _voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++) _voices[i] = CreateSource(host, false);
            _lastPlayed = new float[Enum.GetValues(typeof(SoundId)).Length];
            _played = new int[_lastPlayed.Length];
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = float.NegativeInfinity;
            Subscribe();
        }

        /// <summary>Number of hooks fired (played, limited or silent); handy for tests.</summary>
        public int HooksFired { get; private set; }

        /// <summary>Clips actually started.</summary>
        public int SoundsPlayed { get; private set; }

        public SoundId LastPlayed { get; private set; }

        /// <summary>How often this hook's clip has started (tests).</summary>
        public int TimesPlayed(SoundId id) => (int)id < _played.Length ? _played[(int)id] : 0;

        /// <param name="now">Seconds on any steady clock (unscaled time by default); used for the repeat limit.</param>
        public void Play(SoundId id) => Play(id, Time.unscaledTime);

        public void Play(SoundId id, float now)
        {
            HooksFired++;
            if (_library == null || !_library.TryGetEntry(id, out SoundEntry entry)) return;
            int slot = (int)id;
            if (slot < _lastPlayed.Length)
            {
                if (now - _lastPlayed[slot] < entry.minInterval) return;
                _lastPlayed[slot] = now;
            }

            if (_haptics != null && _settings.VibrationOn) _haptics.Play(entry.haptic);
            if (entry.clip == null || _settings.SoundVolume <= 0f) return;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);
            voice.PlayOneShot(entry.clip, entry.volume * _settings.SoundVolume);
            SoundsPlayed++;
            if (slot < _played.Length) _played[slot]++;
            LastPlayed = id;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SoundRequested>(OnSoundRequested);
            EventBus.Unsubscribe<CoinCollected>(OnCoinCollected);
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Unsubscribe<ProductUnlocked>(OnProductUnlocked);
            EventBus.Unsubscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Unsubscribe<ManagerHired>(OnManagerHired);
            EventBus.Unsubscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Unsubscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Unsubscribe<OfflineRewardClaimed>(OnOfflineRewardClaimed);
            EventBus.Unsubscribe<DailyRewardClaimed>(OnDailyRewardClaimed);
            EventBus.Unsubscribe<AchievementCompleted>(OnAchievementCompleted);
            EventBus.Unsubscribe<BeachEventStarted>(OnBeachEventStarted);
            EventBus.Unsubscribe<MoveEnded>(OnMoveEnded);
        }

        private void Subscribe()
        {
            EventBus.Subscribe<SoundRequested>(OnSoundRequested);
            EventBus.Subscribe<CoinCollected>(OnCoinCollected);
            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            EventBus.Subscribe<ProductUnlocked>(OnProductUnlocked);
            EventBus.Subscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Subscribe<ManagerHired>(OnManagerHired);
            EventBus.Subscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Subscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Subscribe<OfflineRewardClaimed>(OnOfflineRewardClaimed);
            EventBus.Subscribe<DailyRewardClaimed>(OnDailyRewardClaimed);
            EventBus.Subscribe<AchievementCompleted>(OnAchievementCompleted);
            EventBus.Subscribe<BeachEventStarted>(OnBeachEventStarted);
            EventBus.Subscribe<MoveEnded>(OnMoveEnded);
        }

        private void OnSoundRequested(SoundRequested evt) => Play(evt.Id);
        private void OnCoinCollected(CoinCollected evt) => Play(SoundId.CoinCollect);
        private void OnSaleCompleted(SaleCompleted evt) => Play(SoundId.Sale);
        private void OnTierUpgraded(TierUpgraded evt) => Play(SoundId.TierUpgrade);
        private void OnProductUnlocked(ProductUnlocked evt) => Play(SoundId.ProductUnlock);
        private void OnStandUpgraded(StandUpgraded evt) => Play(SoundId.StandUpgrade);
        private void OnManagerHired(ManagerHired evt) => Play(SoundId.Reward);
        private void OnNeighbourMovedIn(NeighbourMovedIn evt) => Play(SoundId.NeighbourMovedIn);
        private void OnQuestCompleted(QuestCompleted evt) => Play(SoundId.QuestComplete);
        private void OnOfflineRewardClaimed(OfflineRewardClaimed evt) => Play(SoundId.Reward);
        private void OnDailyRewardClaimed(DailyRewardClaimed evt) => Play(SoundId.Reward);
        private void OnAchievementCompleted(AchievementCompleted evt) => Play(SoundId.Trophy);
        private void OnBeachEventStarted(BeachEventStarted evt) => Play(SoundId.EventStart);

        private void OnMoveEnded(MoveEnded evt)
        {
            if (evt.Committed) Play(SoundId.Place);
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            // Only the start of construction; completion is covered by TierUpgraded.
            if (ServiceLocator.TryGet(out BuildingManager buildings) && buildings.GetLot(evt.LotIndex).state == LotState.Building)
            {
                Play(SoundId.BuildStart);
            }
        }

        private static AudioSource CreateSource(GameObject host, bool loop)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
