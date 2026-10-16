using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shows the DayClock: tints the sun, ambient light, fog and the camera's sky colour from the
    /// DayNightConfig gradients and swings the sun a little sideways. The scene's own colours are the
    /// midday baseline. Sits on the scene's Sun; allocation-free.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class DayNightView : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;

        private Light _sun;
        private DayClock _clock;
        private DayNightConfig _config;
        private Vector3 _baseAngles;
        private Color _skyAmbient, _equatorAmbient, _groundAmbient, _background;
        private bool _hasBackground;

        /// <summary>Phase shown by the last update (-1 before the first).</summary>
        public float AppliedPhase { get; private set; } = -1f;
        public Color SunColor => _sun.color;
        public float SunIntensity => _sun.intensity;
        public Color FogColor => RenderSettings.fogColor;

        private void Start()
        {
            _sun = GetComponent<Light>();
            if (!ServiceLocator.TryGet(out _clock))
            {
                enabled = false;
                return;
            }

            _config = ServiceLocator.Get<GameDatabase>().dayNight;
            _baseAngles = transform.eulerAngles;
            _skyAmbient = RenderSettings.ambientSkyColor;
            _equatorAmbient = RenderSettings.ambientEquatorColor;
            _groundAmbient = RenderSettings.ambientGroundColor;
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            // The camera controller sets the sea colour as background in its own Start; read it once here.
            if (!_hasBackground && worldCamera != null)
            {
                _background = worldCamera.backgroundColor;
                _hasBackground = true;
            }

            float phase = _clock.Phase;
            _sun.color = _config.sunColor.Evaluate(phase);
            _sun.intensity = _config.sunIntensity.Evaluate(phase);
            float swing = Mathf.Cos(phase * 2f * Mathf.PI) * _config.sunSwingDegrees;
            transform.rotation = Quaternion.Euler(_baseAngles.x, _baseAngles.y + swing, _baseAngles.z);

            Color tint = _config.ambientTint.Evaluate(phase);
            RenderSettings.ambientSkyColor = _skyAmbient * tint;
            RenderSettings.ambientEquatorColor = _equatorAmbient * tint;
            RenderSettings.ambientGroundColor = _groundAmbient * tint;
            RenderSettings.fogColor = _config.fogColor.Evaluate(phase);
            if (_hasBackground) worldCamera.backgroundColor = _background * tint;
            AppliedPhase = phase;
        }
    }
}
