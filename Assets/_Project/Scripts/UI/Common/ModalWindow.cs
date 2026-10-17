using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Sits on a full-screen window root. Scales the window down so it (and its close button) always fits the
    /// screen, e.g. on a wide or short display, and tells input that the map is covered while it is shown.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ModalWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform window = null;
        [Tooltip("Largest share of the screen the window may take on each axis.")]
        [SerializeField, Range(0.5f, 1f)] private float maxScreenFraction = 0.96f;

        private RectTransform _root;
        private bool _announced;

        private void OnEnable()
        {
            _root = (RectTransform)transform;
            Fit();
            _announced = true;
            EventBus.Publish(new ModalVisibilityChanged(true));
        }

        private void OnDisable()
        {
            if (!_announced) return;
            _announced = false;
            EventBus.Publish(new ModalVisibilityChanged(false));
        }

        // Called by Unity when the screen (and so this stretched root) changes size.
        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Fit();
        }

        private void Fit()
        {
            if (window == null || _root == null) return;

            Vector2 space = _root.rect.size * maxScreenFraction;
            Vector2 size = window.rect.size;
            if (size.x <= 0f || size.y <= 0f || space.x <= 0f || space.y <= 0f) return;

            float scale = Mathf.Min(1f, space.x / size.x, space.y / size.y);
            window.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
