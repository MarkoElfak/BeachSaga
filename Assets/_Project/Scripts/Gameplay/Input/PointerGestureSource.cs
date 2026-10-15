using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Reads touches (or the mouse in the editor) through the new Input System and publishes
    /// Tap, Drag and Pinch events. Gestures that start on UI are ignored.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PointerGestureSource : MonoBehaviour
    {
        private const float MinPinchDistancePixels = 1f;
        private const float ScrollDeadZone = 0.01f;

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private PointerEventData _uiPointerData;
        private EventSystem _uiPointerOwner;
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

        public void Initialize(InputConfig config)
        {
            _config = config;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (_config == null)
            {
                return;
            }

            int count = ReadPointers(out Vector2 p0, out Vector2 p1);

            if (count > 0 && _previousCount == 0) BeginGesture(p0);
            else if (count == 0 && _previousCount > 0) EndGesture();
            else if (count == 1 && _previousCount == 1) MoveSingle(p0);
            else if (count >= 2 && _previousCount >= 2) Pinch(p0, p1);
            // A finger count change (1 <-> 2) only re-bases positions, so the view does not jump.

            if (count >= 2) _multiTouch = true;
            if (count > 0) _previous0 = p0;
            if (count > 1) _previous1 = p1;
            _previousCount = count;

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
            _startedOverUi = IsOverUi(position);

            float dpi = Screen.dpi > 0f ? Screen.dpi : _config.fallbackDpi;
            float threshold = _config.tapMaxMoveInches * dpi;
            _dragThresholdSqr = threshold * threshold;
        }

        private void MoveSingle(Vector2 position)
        {
            if (_startedOverUi || _multiTouch)
            {
                return;
            }

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
            if (_startedOverUi)
            {
                return;
            }

            float previousDistance = Vector2.Distance(_previous0, _previous1);
            if (previousDistance < MinPinchDistancePixels)
            {
                return;
            }

            float ratio = Vector2.Distance(p0, p1) / previousDistance;
            EventBus.Publish(new PinchEvent(ratio, (p0 + p1) * 0.5f));
        }

        private void EndGesture()
        {
            bool quick = Time.unscaledTime - _startTime <= _config.tapMaxSeconds;
            if (quick && !_dragging && !_multiTouch && !_startedOverUi)
            {
                EventBus.Publish(new TapEvent(_previous0));
            }
        }

        private void ReadScrollZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < ScrollDeadZone)
            {
                return;
            }

            float step = 1f + _config.scrollZoomStep;
            float ratio = scroll > 0f ? step : 1f / step;
            EventBus.Publish(new PinchEvent(ratio, mouse.position.ReadValue()));
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_uiPointerData == null || _uiPointerOwner != eventSystem)
            {
                _uiPointerData = new PointerEventData(eventSystem);
                _uiPointerOwner = eventSystem;
            }

            _uiPointerData.position = screenPosition;
            _uiHits.Clear();
            eventSystem.RaycastAll(_uiPointerData, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
