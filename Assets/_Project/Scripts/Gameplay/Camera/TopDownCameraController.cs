using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Fixed-angle camera orbiting a focus point on the ground. Drag pans so the ground stays under the finger;
    /// pinch or mouse wheel zooms, stretching softly past the limits and springing back. Full zoom-out is worked
    /// out from the unlocked land and the screen's shape, so it always shows the whole map; a double tap on open
    /// ground glides between that view and a close-up. Haze, shadows and draw distance follow the zoom.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed partial class TopDownCameraController : MonoBehaviour
    {
        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);
        private const float PinchSettleDelay = 0.12f;

        private Camera _camera;
        private CameraConfig _config;
        private Quaternion _rotation;
        private Rect _area;
        private Vector3 _focus;
        private float _distance;
        private float _pinchDistance; // unsoftened distance while a pinch runs
        private float _maxDistance;
        private float _aspect;
        private float _lastPinchTime = float.NegativeInfinity;
        private bool _initialized;

        /// <summary>Current zoom-out limit: the whole unlocked land fits the screen there.</summary>
        public float MaxDistance => _maxDistance;
        public float MinDistance => _config.minDistance;
        public float Distance => _distance;
        public Vector3 Focus => _focus;
        public Rect Area => _area;

        /// <param name="area">The unlocked land: the focus roams over it and full zoom-out shows all of it.</param>
        public void Initialize(CameraConfig config, Rect area, Vector3 startFocus, Color background)
        {
            _camera = GetComponent<Camera>();
            _config = config;
            _rotation = Quaternion.Euler(config.pitchDegrees, config.yawDegrees, 0f);
            _camera.fieldOfView = config.fieldOfView;
            _camera.nearClipPlane = config.nearClip;
            _camera.farClipPlane = config.farClip;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;
            RememberShadowDistance();

            _area = area;
            Refit();
            _distance = Mathf.Clamp(config.startDistance, config.minDistance, _maxDistance);
            _focus = ClampFocus(startFocus, _distance);
            _initialized = true;
            ApplyPose();
        }

        /// <summary>New land was unlocked: the camera may roam over it and zoom out further.</summary>
        public void SetLimits(Rect area)
        {
            _area = area;
            if (!_initialized) return;
            Refit();
            ApplyPose();
        }

        /// <summary>The farthest zoom that shows the whole unlocked land on a screen of this shape.</summary>
        public float FitDistanceFor(float aspect) => FitDistance(_area, aspect);

        // The zoom that shows this ground rectangle (plus the fit margin) on a screen of this shape.
        private float FitDistance(Rect area, float aspect)
        {
            float margin = _config.fitMargin;
            Rect padded = Rect.MinMaxRect(area.xMin - margin, area.yMin - margin, area.xMax + margin, area.yMax + margin);
            float fit = CameraFraming.FitDistance(padded, _rotation, _config.fieldOfView, aspect, _config.fitHeight);
            return Mathf.Max(_config.minDistance, fit);
        }

        private void Refit()
        {
            _aspect = _camera.aspect;
            _maxDistance = FitDistanceFor(_aspect);
            _distance = Mathf.Clamp(_distance, _config.minDistance, _maxDistance);
            _pinchDistance = _distance;
            _focus = ClampFocus(_focus, _distance);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DragEvent>(OnDrag);
            EventBus.Subscribe<PinchEvent>(OnPinch);
            EventBus.Subscribe<DoubleTapEvent>(OnDoubleTap);
            EventBus.Subscribe<CameraShakeRequested>(OnShake);
            EventBus.Subscribe<CameraFocusRequested>(OnFocusRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DragEvent>(OnDrag);
            EventBus.Unsubscribe<PinchEvent>(OnPinch);
            EventBus.Unsubscribe<DoubleTapEvent>(OnDoubleTap);
            EventBus.Unsubscribe<CameraShakeRequested>(OnShake);
            EventBus.Unsubscribe<CameraFocusRequested>(OnFocusRequested);
            RestoreShadowDistance();
        }

        private void OnDrag(DragEvent evt)
        {
            if (!_initialized) return;
            if (TryGetGroundPoint(evt.From, out Vector3 from) && TryGetGroundPoint(evt.To, out Vector3 to))
            {
                StopFlying();
                _focus = ClampFocus(_focus + (from - to), _distance);
                ApplyPose();
            }
        }

        // Past a limit the zoom stretches less and less (it eases to a stop), then springs back in LateUpdate.
        private void OnPinch(PinchEvent evt)
        {
            if (!_initialized || evt.Ratio <= 0f) return;
            StopFlying();
            if (Time.unscaledTime - _lastPinchTime > PinchSettleDelay) _pinchDistance = _distance;
            _lastPinchTime = Time.unscaledTime;

            float min = _config.minDistance;
            float give = _config.zoomGive;
            _pinchDistance = Mathf.Clamp(_pinchDistance / evt.Ratio, min * 0.5f, _maxDistance * 2f);
            _distance = CameraFraming.Soften(_pinchDistance, min, _maxDistance, min * give, _maxDistance * give);
            _focus = ClampFocus(_focus, _distance);
            ApplyPose();
        }

        private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (Ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = default;
            return false;
        }

        private Vector3 ClampFocus(Vector3 focus, float distance)
        {
            float zoomOut = Mathf.InverseLerp(_config.minDistance, _maxDistance, distance);
            Rect bounds = CameraFraming.FocusBounds(_area, _config.edgeLook, zoomOut);
            return new Vector3(Mathf.Clamp(focus.x, bounds.xMin, bounds.xMax), 0f, Mathf.Clamp(focus.z, bounds.yMin, bounds.yMax));
        }

        private void ApplyPose()
        {
            Vector3 position = _focus - _rotation * Vector3.forward * _distance;
            if (IsShaking) position += _rotation * ShakeOffset();
            transform.SetPositionAndRotation(position, _rotation);
            FollowZoom();
        }
    }
}
