using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Reads touches (or the mouse in the editor) through the new Input System and publishes Tap, Drag, Pinch
    /// and HoldStarted (long press). A listener may capture the pointer after a hold (e.g. to move a building):
    /// the camera then stops panning and CapturedPointerMoved / CapturedPointerReleased are sent instead.
    /// Gestures that start on UI are ignored, and nothing reaches the map while a full-screen window is open.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PointerGestureSource : MonoBehaviour
    {
        private const float MinPinchDistancePixels = 1f;
        private const float ScrollDeadZone = 0.01f;

        private readonly UiPointerProbe _ui = new UiPointerProbe();
        private InputConfig _config;

        private int _previousCount;
        private Vector2 _previous0;
        private Vector2 _previous1;
        private Vector2 _start;
        private float _startTime;
        private float _dragThresholdSqr;
        private bool _dragging;
        private bool _multiTouch;
        private bool _startedOverUi;
        private bool _holdFired;
        private bool _captured;
        private int _openModals;

        public void Initialize(InputConfig config)
        {
            _config = config;
        }

        /// <summary>Call from a HoldStarted handler to take over the rest of this press.</summary>
        public void CapturePointer() => _captured = _previousCount > 0;

        public bool IsCaptured => _captured;

        /// <summary>True while a window covers the map.</summary>
        public bool IsBlocked => _openModals > 0;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            EventBus.Subscribe<ModalVisibilityChanged>(OnModal);
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            EventBus.Unsubscribe<ModalVisibilityChanged>(OnModal);
        }

        // A window opening mid-gesture turns the rest of that gesture into a UI gesture (no tap, drag or hold).
        private void OnModal(ModalVisibilityChanged evt)
        {
            _openModals = Mathf.Max(0, _openModals + (evt.Open ? 1 : -1));
            if (evt.Open) _startedOverUi = true;
        }

        private void Update()
        {
            if (_config == null) return;

            int count = ReadPointers(out Vector2 p0, out Vector2 p1);

            if (count > 0 && _previousCount == 0) BeginGesture(p0);
            else if (count == 0 && _previousCount > 0) EndGesture();
            else if (count == 1 && _previousCount == 1) MoveSingle(p0);
            else if (count >= 2 && _previousCount >= 2 && !_captured) Pinch(p0, p1);
            // A finger count change (1 <-> 2) only re-bases positions, so the view does not jump.

            if (count >= 2) _multiTouch = true;
            if (count > 0) _previous0 = p0;
            if (count > 1) _previous1 = p1;
            _previousCount = count;

            if (count == 1) CheckHold(p0);
            ReadScrollZoom();
        }

        private static int ReadPointers(out Vector2 p0, out Vector2 p1)
        {
            p0 = default;
            p1 = default;
            int count = 0;

            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                Touch touch = touches[i];
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                if (count == 0) p0 = touch.screenPosition;
                else if (count == 1) p1 = touch.screenPosition;
                count++;
            }

            if (count > 0)
            {
                return count;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                p0 = mouse.position.ReadValue();
                return 1;
            }

            return 0;
        }

        private void BeginGesture(Vector2 position)
        {
            _start = position;
            _startTime = Time.unscaledTime;
            _dragging = false;
            _multiTouch = false;
            _holdFired = false;
            _captured = false;
            _startedOverUi = IsBlocked || _ui.IsOverUi(position);

            float dpi = Screen.dpi > 0f ? Screen.dpi : _config.fallbackDpi;
            float threshold = _config.tapMaxMoveInches * dpi;
            _dragThresholdSqr = threshold * threshold;
        }

        // A press that stays still long enough becomes a long press (only once per gesture).
        private void CheckHold(Vector2 position)
        {
            bool eligible = !_holdFired && !_dragging && !_multiTouch && !_startedOverUi;
            if (eligible && Time.unscaledTime - _startTime >= _config.holdToMoveSeconds)
            {
                _holdFired = true;
                EventBus.Publish(new HoldStarted(position));
            }
        }

        private void MoveSingle(Vector2 position)
        {
            if (_captured)
            {
                EventBus.Publish(new CapturedPointerMoved(position));
                return;
            }

            if (_startedOverUi || _multiTouch) return;

            if (!_dragging && (position - _start).sqrMagnitude > _dragThresholdSqr)
            {
                _dragging = true;
            }

            if (_dragging)
            {
                EventBus.Publish(new DragEvent(_previous0, position));
            }
        }

        private void Pinch(Vector2 p0, Vector2 p1)
        {
            if (_startedOverUi) return;

            float previousDistance = Vector2.Distance(_previous0, _previous1);
            if (previousDistance < MinPinchDistancePixels) return;

            float ratio = Vector2.Distance(p0, p1) / previousDistance;
            EventBus.Publish(new PinchEvent(ratio, (p0 + p1) * 0.5f));
        }

        private void EndGesture()
        {
            if (_captured)
            {
                _captured = false;
                EventBus.Publish(new CapturedPointerReleased(_previous0));
                return;
            }

            bool quick = Time.unscaledTime - _startTime <= _config.tapMaxSeconds;
            if (quick && !_dragging && !_multiTouch && !_startedOverUi)
            {
                EventBus.Publish(new TapEvent(_previous0));
            }
        }

        private void ReadScrollZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < ScrollDeadZone || IsBlocked || _ui.IsOverUi(mouse.position.ReadValue())) return;

            float step = 1f + _config.scrollZoomStep;
            float ratio = scroll > 0f ? step : 1f / step;
            EventBus.Publish(new PinchEvent(ratio, mouse.position.ReadValue()));
        }
    }
}
