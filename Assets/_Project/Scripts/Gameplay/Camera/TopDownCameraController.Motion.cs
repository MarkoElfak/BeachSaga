using BeachTycoon.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeachTycoon.Gameplay
{
    // Movement over time: zoom spring-back, double-tap glides, shakes, screen rotation, and the haze, shadows
    // and draw distance that follow the zoom.
    public sealed partial class TopDownCameraController
    {
        private const float CloseEnoughToWhole = 0.9f; // share of full zoom-out that counts as the whole-map view

        private float _shakeLeft;
        private float _shakeStrength;
        private bool _flying;
        private float _flyTime;
        private Vector3 _flyFromFocus;
        private Vector3 _flyToFocus;
        private float _flyFromDistance;
        private float _flyToDistance;
        private float _shadowDistanceBefore = -1f;

        public bool IsShaking => _shakeLeft > 0f;
        public bool IsFlying => _flying;

        /// <summary>True when the camera rests at (or close to) full zoom-out.</summary>
        public bool ShowsWholeMap => _distance >= _maxDistance * CloseEnoughToWhole;

        /// <summary>Shakes requested so far (tests).</summary>
        public int ShakeCount { get; private set; }

        /// <summary>Glides to full zoom-out over the centre of the land.</summary>
        public void FlyToWholeMap() => FlyTo(new Vector3(_area.center.x, 0f, _area.center.y), _maxDistance);

        /// <summary>Glides to the usual close-up distance over a ground point.</summary>
        public void FlyToCloseUp(Vector3 point)
        {
            float distance = Mathf.Clamp(_config.startDistance, _config.minDistance, _maxDistance);
            FlyTo(ClampFocus(point, distance), distance);
        }

        /// <summary>Glides over newly opened land, zoomed out just enough to show all of it.</summary>
        public void FlyToArea(Rect area)
        {
            if (!_initialized) return;
            float distance = Mathf.Clamp(FitDistance(area, _aspect), Mathf.Min(_config.startDistance, _maxDistance), _maxDistance);
            FlyTo(ClampFocus(new Vector3(area.center.x, 0f, area.center.y), distance), distance);
        }

        private void FlyTo(Vector3 focus, float distance)
        {
            if (!_initialized) return;
            _flyFromFocus = _focus;
            _flyFromDistance = _distance;
            _flyToFocus = focus;
            _flyToDistance = distance;
            _flyTime = 0f;
            _flying = true;
        }

        private void StopFlying() => _flying = false;

        private void OnDoubleTap(DoubleTapEvent evt)
        {
            if (!_initialized) return;
            if (!ShowsWholeMap) FlyToWholeMap();
            else if (TryGetGroundPoint(evt.ScreenPosition, out Vector3 point)) FlyToCloseUp(point);
        }

        private void OnFocusRequested(CameraFocusRequested evt)
        {
            if (_initialized) FlyToCloseUp(evt.Point);
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
            if (!_initialized) return;
            bool moved = false;
            if (!Mathf.Approximately(_camera.aspect, _aspect))
            {
                Refit(); // the screen turned or the window changed shape
                moved = true;
            }

            float dt = Time.unscaledDeltaTime;
            moved |= Glide(dt) || SpringBack(dt);
            if (IsShaking)
            {
                _shakeLeft = Mathf.Max(0f, _shakeLeft - dt);
                moved = true; // the last pose, with no shake left, settles back on the exact spot
            }

            if (moved) ApplyPose();
        }

        private bool Glide(float dt)
        {
            if (!_flying) return false;
            _flyTime += dt;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_flyTime / _config.flySeconds));
            _distance = Mathf.Lerp(_flyFromDistance, _flyToDistance, t);
            _focus = Vector3.Lerp(_flyFromFocus, _flyToFocus, t);
            _pinchDistance = _distance;
            if (t >= 1f) _flying = false;
            return true;
        }

        // Once the fingers stop, a zoom stretched past a limit eases back onto it.
        private bool SpringBack(float dt)
        {
            float limited = Mathf.Clamp(_distance, _config.minDistance, _maxDistance);
            if (Mathf.Approximately(limited, _distance) || Time.unscaledTime - _lastPinchTime < PinchSettleDelay) return false;
            _distance = Mathf.Abs(_distance - limited) < 0.01f ? limited
                : Mathf.Lerp(_distance, limited, 1f - Mathf.Exp(-_config.zoomSettleSpeed * dt));
            _pinchDistance = _distance;
            _focus = ClampFocus(_focus, _distance);
            return true;
        }

        // Smooth noise on the screen's x and y, fading out over the shake.
        private Vector3 ShakeOffset()
        {
            float fade = _shakeLeft / _config.shakeSeconds;
            float amplitude = _config.shakeAmplitude * _shakeStrength * fade * 2f;
            float time = Time.unscaledTime * _config.shakeFrequency;
            return new Vector3(Mathf.PerlinNoise(time, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, time) - 0.5f, 0f) * amplitude;
        }

        // The overview stays clear (haze starts beyond the map) and keeps its shadows; close up stays as before.
        private void FollowZoom()
        {
            float hazeStart = _distance + _config.hazeStartAhead;
            float hazeEnd = hazeStart + _config.hazeLength;
            RenderSettings.fogStartDistance = hazeStart;
            RenderSettings.fogEndDistance = hazeEnd;
            _camera.farClipPlane = Mathf.Max(_config.farClip, hazeEnd + 10f);
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.shadowDistance = Mathf.Max(_config.minShadowDistance, _distance + _config.shadowAhead);
            }
        }

        // The pipeline asset is shared (and saved in the editor), so its shadow distance is put back afterwards.
        private void RememberShadowDistance()
        {
            if (_shadowDistanceBefore < 0f && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                _shadowDistanceBefore = pipeline.shadowDistance;
            }
        }

        private void RestoreShadowDistance()
        {
            if (_shadowDistanceBefore >= 0f && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.shadowDistance = _shadowDistanceBefore;
            }

            _shadowDistanceBefore = -1f;
        }
    }
}
