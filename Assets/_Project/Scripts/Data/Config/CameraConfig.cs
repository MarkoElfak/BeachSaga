using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Fixed top-down camera framing and zoom limits.</summary>
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
        [Min(1f)] public float minDistance = 18f;
        [Min(1f)] public float maxDistance = 60f;

        [Tooltip("Shrinks the land rectangle the focus point is clamped to.")]
        [Min(0f)] public float focusBoundsInset = 2f;

        [Header("Shake (a building finishes)")]
        [Tooltip("Largest camera offset in metres at full strength.")]
        [Min(0f)] public float shakeAmplitude = 0.35f;
        [Min(0.05f)] public float shakeSeconds = 0.45f;
        [Tooltip("Wobbles per second.")]
        [Min(1f)] public float shakeFrequency = 18f;
    }
}
