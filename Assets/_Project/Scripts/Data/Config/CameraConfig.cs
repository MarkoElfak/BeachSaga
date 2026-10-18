using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Fixed-angle camera framing, zoom feel, and the haze and shadows that follow the zoom.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Camera", fileName = "CameraConfig")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Tooltip("Camera X rotation. 90 looks straight down.")]
        [Range(10f, 90f)] public float pitchDegrees = 55f;
        public float yawDegrees;

        [Tooltip("Vertical field of view. Keep it narrow for a near-orthographic look.")]
        [Range(10f, 60f)] public float fieldOfView = 35f;

        [Min(0.01f)] public float nearClip = 0.3f;
        [Min(1f)] public float farClip = 250f;

        [Tooltip("Distance from the focus point on the ground to the camera.")]
        [Min(1f)] public float startDistance = 34f;
        [Tooltip("Closest zoom. The farthest is worked out from the unlocked land and the screen's shape.")]
        [Min(1f)] public float minDistance = 18f;

        [Header("Whole-map view")]
        [Tooltip("Ground around the unlocked land that full zoom-out still shows, in metres.")]
        [Min(0f)] public float fitMargin = 1.5f;
        [Tooltip("Height of the tallest buildings, kept in view at full zoom-out.")]
        [Min(0f)] public float fitHeight = 4f;
        [Tooltip("How far past the land's edge the view may be dragged when zoomed in, in metres.")]
        [Min(0f)] public float edgeLook = 3f;

        [Header("Feel")]
        [Tooltip("How far a pinch may stretch past a zoom limit (share of the limit) before it springs back.")]
        [Range(0f, 0.3f)] public float zoomGive = 0.08f;
        [Tooltip("How quickly a stretched zoom springs back once the fingers stop.")]
        [Min(1f)] public float zoomSettleSpeed = 10f;
        [Tooltip("Double tap on open ground: seconds to glide between the whole map and a close-up.")]
        [Min(0.05f)] public float flySeconds = 0.5f;

        [Header("Haze, shadows and draw distance follow the zoom")]
        [Tooltip("Haze starts this far beyond the focus point.")]
        [Min(0f)] public float hazeStartAhead = 20f;
        [Min(1f)] public float hazeLength = 115f;
        [Tooltip("Shadows are drawn up to this far beyond the focus point (never less than minShadowDistance).")]
        [Min(0f)] public float shadowAhead = 36f;
        [Min(1f)] public float minShadowDistance = 70f;

        [Header("Shake (a building finishes)")]
        [Tooltip("Largest camera offset in metres at full strength.")]
        [Min(0f)] public float shakeAmplitude = 0.35f;
        [Min(0.05f)] public float shakeSeconds = 0.45f;
        [Tooltip("Wobbles per second.")]
        [Min(1f)] public float shakeFrequency = 18f;
    }
}
