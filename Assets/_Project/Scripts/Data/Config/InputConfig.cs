using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Gesture thresholds for tap, drag and zoom.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Input", fileName = "InputConfig")]
    public sealed class InputConfig : ScriptableObject
    {
        [Tooltip("A press longer than this is never a tap.")]
        [Min(0.05f)] public float tapMaxSeconds = 0.35f;

        [Tooltip("Finger travel (inches) before a press becomes a drag.")]
        [Min(0f)] public float tapMaxMoveInches = 0.08f;

        [Tooltip("Used when the device does not report its DPI.")]
        [Min(1f)] public float fallbackDpi = 160f;

        [Tooltip("Zoom ratio applied per mouse-wheel notch (editor and desktop).")]
        [Min(0f)] public float scrollZoomStep = 0.12f;

        [Min(1f)] public float raycastMaxDistance = 500f;

        [Tooltip("Two taps on open ground within this time make a double tap (whole map / close-up).")]
        [Min(0.05f)] public float doubleTapSeconds = 0.3f;
        [Tooltip("The second tap of a double tap lands within this many inches of the first.")]
        [Min(0f)] public float doubleTapMaxInches = 0.4f;

        [Tooltip("Press and hold this long on a building or decoration to pick it up and move it.")]
        [Min(0.1f)] public float holdToMoveSeconds = 0.45f;
    }
}
