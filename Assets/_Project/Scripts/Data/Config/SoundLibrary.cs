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
    }

    [System.Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>Maps sound hooks to clips. Drop AudioClips into Audio/ and assign them here; no code changes needed.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        public SoundEntry[] sounds;
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Tooltip("Simultaneous one-shot sounds.")]
        [Min(1)] public int voices = 6;

        public bool TryGet(SoundId id, out SoundEntry entry)
        {
            if (sounds != null)
            {
                for (int i = 0; i < sounds.Length; i++)
                {
                    if (sounds[i].id == id && sounds[i].clip != null)
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
