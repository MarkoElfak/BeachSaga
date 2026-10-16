using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Fits this RectTransform to Screen.safeArea (notches, rounded corners, home indicator).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _applied = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var size = new Vector2(Screen.width, Screen.height);
            _rect.anchorMin = _applied.position / size;
            _rect.anchorMax = (_applied.position + _applied.size) / size;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
