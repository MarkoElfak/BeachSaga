using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Fixed-angle camera orbiting a focus point on the ground. Drag pans so the ground stays under the
    /// finger; pinch or mouse wheel changes distance. The focus point is clamped to the unlocked land, and the
    /// zoom-out limit grows with the main building's tier. A CameraShakeRequested wobbles it briefly (unscaled
    /// time) around the same pose.
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
        private float _maxDistance;
        private bool _initialized;
        private float _shakeLeft;
        private float _shakeStrength;

        /// <param name="area">Land the focus point may move over (the unlocked area).</param>
        /// <param name="maxDistance">How far the player may zoom out for now.</param>
        /// <summary>Current zoom-out limit (grows as land is unlocked).</summary>
        public float MaxDistance => _maxDistance;

        public bool IsShaking => _shakeLeft > 0f;

        /// <summary>Shakes requested so far (tests).</summary>
        public int ShakeCount { get; private set; }

        public void Initialize(CameraConfig config, Rect area, float maxDistance, Vector3 startFocus, Color background)
        {
            _camera = GetComponent<Camera>();
            _config = config;
            _camera.fieldOfView = config.fieldOfView;
            _camera.nearClipPlane = config.nearClip;
            _camera.farClipPlane = config.farClip;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;

            SetBounds(area, maxDistance);
            _distance = Mathf.Clamp(config.startDistance, config.minDistance, _maxDistance);
            _focus = ClampFocus(startFocus);
            _initialized = true;
            ApplyPose();
        }

        /// <summary>New land was unlocked: the camera may roam over it and zoom out further.</summary>
        public void SetLimits(Rect area, float maxDistance)
        {
            SetBounds(area, maxDistance);
            if (!_initialized) return;
            _distance = Mathf.Clamp(_distance, _config.minDistance, _maxDistance);
            _focus = ClampFocus(_focus);
            ApplyPose();
        }

        private void SetBounds(Rect area, float maxDistance)
        {
            float inset = _config.focusBoundsInset;
            float insetX = Mathf.Min(inset, area.width * 0.5f);
            float insetZ = Mathf.Min(inset, area.height * 0.5f);
            _focusBounds = Rect.MinMaxRect(area.xMin + insetX, area.yMin + insetZ, area.xMax - insetX, area.yMax - insetZ);
            _maxDistance = Mathf.Max(_config.minDistance, maxDistance > 0f ? maxDistance : _config.maxDistance);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DragEvent>(OnDrag);
            EventBus.Subscribe<PinchEvent>(OnPinch);
            EventBus.Subscribe<CameraShakeRequested>(OnShake);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DragEvent>(OnDrag);
            EventBus.Unsubscribe<PinchEvent>(OnPinch);
            EventBus.Unsubscribe<CameraShakeRequested>(OnShake);
        }

        private void OnShake(CameraShakeRequested evt)
        {
            if (!_initialized || evt.Strength <= 0f) return;
            ShakeCount++;
            _shakeStrength = IsShaking ? Mathf.Max(_shakeStrength, evt.Strength) : evt.Strength;
            _shakeLeft = _config.shakeSeconds;
        }

        private void LateUpdate()
        {
            if (!IsShaking) return;
            _shakeLeft = Mathf.Max(0f, _shakeLeft - Time.unscaledDeltaTime);
            ApplyPose(); // the last call, with no shake left, settles back on the exact pose
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

            _distance = Mathf.Clamp(_distance / evt.Ratio, _config.minDistance, _maxDistance);
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
            if (IsShaking) position += rotation * ShakeOffset();
            transform.SetPositionAndRotation(position, rotation);
        }

        // Smooth noise on the screen's x and y, fading out over the shake.
        private Vector3 ShakeOffset()
        {
            float fade = _shakeLeft / _config.shakeSeconds;
            float amplitude = _config.shakeAmplitude * _shakeStrength * fade * 2f;
            float time = Time.unscaledTime * _config.shakeFrequency;
            return new Vector3(Mathf.PerlinNoise(time, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, time) - 0.5f, 0f) * amplitude;
        }
    }
}
