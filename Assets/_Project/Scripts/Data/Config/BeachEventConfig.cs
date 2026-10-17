using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>When random beach events happen (in play time) and which ones exist.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Beach Events", fileName = "BeachEventConfig")]
    public sealed class BeachEventConfig : ScriptableObject
    {
        [Tooltip("Play seconds after launch before the first event can start.")]
        [Min(0f)] public float firstEventSeconds = 120f;
        [Tooltip("Play seconds between the end of one event and the start of the next (random in range).")]
        public Vector2 gapSecondsRange = new Vector2(180f, 360f);
        public int randomSeed = 4321;
        public BeachEventDefinition[] events;
    }
}
