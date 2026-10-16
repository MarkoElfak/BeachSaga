using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
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
    /// Audio hooks: maps gameplay events to SoundIds and plays whatever clip the SoundLibrary assigns
    /// (silence when none). A small round-robin pool of 2D AudioSources avoids per-sound allocations.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private readonly SoundLibrary _library;
        private readonly SettingsService _settings;
        private readonly AudioSource[] _voices;
        private readonly AudioSource _music;
        private int _nextVoice;

        public AudioService(SoundLibrary library, SettingsService settings, GameObject host)
        {
            _library = library;
            _settings = settings;
            int voiceCount = library != null ? Mathf.Max(1, library.voices) : 1;
            _voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                _voices[i] = CreateSource(host, false);
            }

            _music = CreateSource(host, true);
            Subscribe();
            ApplyMusic();
        }

        /// <summary>Number of hooks fired (played or silent); handy for tests.</summary>
        public int HooksFired { get; private set; }

        public void Play(SoundId id)
        {
            HooksFired++;
            if (!_settings.SoundOn || _library == null || !_library.TryGet(id, out SoundEntry entry))
            {
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.PlayOneShot(entry.clip, entry.volume);
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
            EventBus.Unsubscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Unsubscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Unsubscribe<OfflineRewardClaimed>(OnOfflineRewardClaimed);
            EventBus.Unsubscribe<SettingsChanged>(OnSettingsChanged);
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
            EventBus.Subscribe<NeighbourMovedIn>(OnNeighbourMovedIn);
            EventBus.Subscribe<QuestCompleted>(OnQuestCompleted);
            EventBus.Subscribe<OfflineRewardClaimed>(OnOfflineRewardClaimed);
            EventBus.Subscribe<SettingsChanged>(OnSettingsChanged);
        }

        private void OnSoundRequested(SoundRequested evt) => Play(evt.Id);
        private void OnCoinCollected(CoinCollected evt) => Play(SoundId.CoinCollect);
        private void OnSaleCompleted(SaleCompleted evt) => Play(SoundId.Sale);
        private void OnTierUpgraded(TierUpgraded evt) => Play(SoundId.TierUpgrade);
        private void OnProductUnlocked(ProductUnlocked evt) => Play(SoundId.ProductUnlock);
        private void OnStandUpgraded(StandUpgraded evt) => Play(SoundId.StandUpgrade);
        private void OnNeighbourMovedIn(NeighbourMovedIn evt) => Play(SoundId.NeighbourMovedIn);
        private void OnQuestCompleted(QuestCompleted evt) => Play(SoundId.QuestComplete);
        private void OnOfflineRewardClaimed(OfflineRewardClaimed evt) => Play(SoundId.Reward);
        private void OnSettingsChanged(SettingsChanged evt) => ApplyMusic();

        private void OnLotStateChanged(LotStateChanged evt)
        {
            // Only the start of construction; completion is covered by TierUpgraded.
            if (ServiceLocator.TryGet(out BuildingManager buildings) && buildings.GetLot(evt.LotIndex).state == LotState.Building)
            {
                Play(SoundId.BuildStart);
            }
        }

        private void ApplyMusic()
        {
            AudioClip clip = _library != null ? _library.music : null;
            bool shouldPlay = clip != null && _settings.MusicOn;
            if (!shouldPlay)
            {
                _music.Stop();
                return;
            }

            if (_music.clip != clip) _music.clip = clip;
            _music.volume = _library.musicVolume;
            if (!_music.isPlaying) _music.Play();
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
