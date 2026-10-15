using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Fixed-angle camera orbiting a focus point on the ground. Drag pans so the ground stays under the
    /// finger; pinch or mouse wheel changes distance. The focus point is clamped to the island.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);

        private Camera _camera;
        private CameraConfig _config;
        private Rect _focusBounds;
        private Vector3 _focus;
        private float _distance;
        private bool _initialized;

        public void Initialize(CameraConfig config, Rect landBounds, Vector3 startFocus, Color background)
        {
            _camera = GetComponent<Camera>();
            _config = config;

            float inset = config.focusBoundsInset;
            float insetX = Mathf.Min(inset, landBounds.width * 0.5f);
            float insetZ = Mathf.Min(inset, landBounds.height * 0.5f);
            _focusBounds = Rect.MinMaxRect(
                landBounds.xMin + insetX, landBounds.yMin + insetZ,
                landBounds.xMax - insetX, landBounds.yMax - insetZ);

            _camera.fieldOfView = config.fieldOfView;
            _camera.nearClipPlane = config.nearClip;
            _camera.farClipPlane = config.farClip;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;

            _distance = Mathf.Clamp(config.startDistance, config.minDistance, config.maxDistance);
            _focus = ClampFocus(startFocus);
            _initialized = true;
            ApplyPose();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DragEvent>(OnDrag);
            EventBus.Subscribe<PinchEvent>(OnPinch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DragEvent>(OnDrag);
            EventBus.Unsubscribe<PinchEvent>(OnPinch);
        }

        private void OnDrag(DragEvent evt)
        {
            if (!_initialized)
            {
                return;
            }

            if (TryGetGroundPoint(evt.From, out Vector3 from) && TryGetGroundPoint(evt.To, out Vector3 to))
            {
                _focus = ClampFocus(_focus + (from - to));
                ApplyPose();
            }
        }

        private void OnPinch(PinchEvent evt)
        {
            if (!_initialized || evt.Ratio <= 0f)
            {
                return;
            }

            _distance = Mathf.Clamp(_distance / evt.Ratio, _config.minDistance, _config.maxDistance);
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

        private Vector3 ClampFocus(Vector3 focus)
        {
            return new Vector3(
                Mathf.Clamp(focus.x, _focusBounds.xMin, _focusBounds.xMax),
                0f,
                Mathf.Clamp(focus.z, _focusBounds.yMin, _focusBounds.yMax));
        }

        private void ApplyPose()
        {
            Quaternion rotation = Quaternion.Euler(_config.pitchDegrees, _config.yawDegrees, 0f);
            Vector3 position = _focus - rotation * Vector3.forward * _distance;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
