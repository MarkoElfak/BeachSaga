using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// The day and night cycle in play time. Phase 0 is dawn, 0.25 midday, 0.5 sunset and 0.75 midnight.
    /// Only the sun, ambient light, fog and sky colour change; nothing is baked.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Day Night", fileName = "DayNightConfig")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [Tooltip("Play minutes for one full day and night.")]
        [Min(0.1f)] public float cycleMinutes = 12f;
        [Tooltip("Phase a new game starts at.")]
        [Range(0f, 1f)] public float startPhase = 0.1f;

        public Gradient sunColor = Make(new Color(1f, 0.72f, 0.55f), new Color(1f, 0.94f, 0.84f), new Color(1f, 0.58f, 0.38f), new Color(0.5f, 0.6f, 0.95f));
        public AnimationCurve sunIntensity = Curve(0.85f, 1.35f, 0.9f, 0.45f);
        [Tooltip("Multiplies the scene's ambient light and sky colour.")]
        public Gradient ambientTint = Make(new Color(0.95f, 0.85f, 0.85f), Color.white, new Color(0.95f, 0.8f, 0.75f), new Color(0.5f, 0.56f, 0.8f));
        public Gradient fogColor = Make(new Color(0.88f, 0.8f, 0.78f), new Color(0.72f, 0.85f, 0.93f), new Color(0.92f, 0.7f, 0.6f), new Color(0.27f, 0.33f, 0.5f));
        [Tooltip("How far the sun swings sideways over the day (degrees).")]
        [Range(0f, 90f)] public float sunSwingDegrees = 40f;

        // Keys: dawn, midday (held until afternoon), sunset, night (held until just before dawn).
        private static Gradient Make(Color dawn, Color day, Color sunset, Color night)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(dawn, 0f), new GradientColorKey(day, 0.12f), new GradientColorKey(day, 0.38f),
                new GradientColorKey(sunset, 0.5f), new GradientColorKey(night, 0.6f), new GradientColorKey(night, 0.9f),
                new GradientColorKey(dawn, 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static AnimationCurve Curve(float dawn, float day, float sunset, float night) => new AnimationCurve(
            new Keyframe(0f, dawn), new Keyframe(0.12f, day), new Keyframe(0.38f, day), new Keyframe(0.5f, sunset),
            new Keyframe(0.6f, night), new Keyframe(0.9f, night), new Keyframe(1f, dawn));
    }
}
