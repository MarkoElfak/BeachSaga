using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Sound hooks raised by gameplay. Clips are optional; an empty slot is simply silent.</summary>
    public enum SoundId
    {
        CoinCollect = 0,
        Sale = 1,
        BuildStart = 2,
        TierUpgrade = 3,
        ProductUnlock = 4,
        StandUpgrade = 5,
        NeighbourMovedIn = 6,
        QuestComplete = 7,
        Reward = 8,
        UiClick = 9,
        MiniGameHit = 10,
        MiniGameMiss = 11,
        MiniGameWin = 12,
        MiniGameLose = 13,
        Trophy = 14,
        EventStart = 15,
        Place = 16,
    }

    /// <summary>How strongly the phone vibrates with a sound hook (None: not at all).</summary>
    public enum HapticStrength
    {
        None = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3,
    }

    [System.Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        [Tooltip("Random pitch change per play (0.1 = up to 10% higher or lower), so repeats sound less samey.")]
        [Range(0f, 0.3f)] public float pitchJitter;
        [Tooltip("Seconds before this sound may play again; extra hooks in between stay silent (busy beaches).")]
        [Min(0f)] public float minInterval;
        public HapticStrength haptic;
    }

    /// <summary>
    /// Maps sound hooks to clips, volume, pitch variation and vibration. Setup fills empty slots with generated
    /// placeholder clips (Audio/Generated); assign your own clips here and Setup leaves them alone.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        public SoundEntry[] sounds;
        [Tooltip("Daytime music loop.")]
        public AudioClip music;
        [Tooltip("Night music loop; the day loop plays at night when empty.")]
        public AudioClip nightMusic;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Tooltip("Seconds to crossfade between the day and night music.")]
        [Min(0.1f)] public float musicFadeSeconds = 4f;
        [Tooltip("Simultaneous one-shot sounds.")]
        [Min(1)] public int voices = 6;

        /// <summary>The slot for this hook with a clip assigned.</summary>
        public bool TryGet(SoundId id, out SoundEntry entry) => TryGetEntry(id, out entry) && entry.clip != null;

        /// <summary>The slot for this hook, with or without a clip (vibration still applies).</summary>
        public bool TryGetEntry(SoundId id, out SoundEntry entry)
        {
            if (sounds != null)
            {
                for (int i = 0; i < sounds.Length; i++)
                {
                    if (sounds[i].id == id)
                    {
                        entry = sounds[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }
    }
}
