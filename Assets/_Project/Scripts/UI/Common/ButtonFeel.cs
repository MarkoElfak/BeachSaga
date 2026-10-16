using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Makes a button feel pressable: it squashes while held, springs back with a small overshoot on release and
    /// clicks when tapped.
    /// Unscaled time; an idle button returns from Update at once (the component must stay enabled to get
    /// pointer events).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.92f;
        [Tooltip("Spring stiffness; higher snaps back faster.")]
        [SerializeField, Min(1f)] private float stiffness = 320f;
        [SerializeField, Min(0f)] private float damping = 16f;

        private Selectable _selectable;
        private float _scale = 1f;
        private float _velocity;
        private bool _held;
        private bool _animating;

        public bool IsHeld => _held;
        public float CurrentScale => _scale;

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnDisable()
        {
            _held = false;
            _animating = false;
            _scale = 1f;
            _velocity = 0f;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable()) return;
            _held = true;
            _animating = true;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_selectable == null || _selectable.IsInteractable()) EventBus.Publish(new SoundRequested(SoundId.UiClick));
        }

        private void Release()
        {
            if (!_held) return;
            _held = false;
            _animating = true;
        }

        private void Update()
        {
            if (!_animating) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float target = _held ? pressedScale : 1f;
            _velocity += ((target - _scale) * stiffness - _velocity * damping) * dt;
            _scale += _velocity * dt;
            if (!_held && Mathf.Abs(_scale - 1f) < 0.001f && Mathf.Abs(_velocity) < 0.01f)
            {
                _scale = 1f;
                _velocity = 0f;
                _animating = false;
            }

            transform.localScale = new Vector3(_scale, _scale, 1f);
        }
    }
}
