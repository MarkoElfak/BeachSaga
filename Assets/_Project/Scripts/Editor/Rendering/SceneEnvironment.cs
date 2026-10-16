using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeachTycoon.EditorTools
{
    /// <summary>Warm late-morning sun, sky/ground ambient, light haze and a global post-processing volume.</summary>
    internal static class SceneEnvironment
    {
        private static readonly Color SunColor = new Color(1f, 0.94f, 0.84f);
        private static readonly Color SkyAmbient = new Color(0.62f, 0.76f, 0.95f);
        private static readonly Color HorizonAmbient = new Color(0.78f, 0.76f, 0.7f);
        private static readonly Color GroundAmbient = new Color(0.52f, 0.46f, 0.38f);
        private static readonly Color Haze = new Color(0.72f, 0.85f, 0.93f);

        public static void Build(Camera camera)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = SkyAmbient;
            RenderSettings.ambientEquatorColor = HorizonAmbient;
            RenderSettings.ambientGroundColor = GroundAmbient;
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Haze;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 170f;

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.shadowNormalBias = 0.6f;
            light.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            RenderSettings.sun = light;

            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = RenderingAssets.PostProcessingProfile();

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            data.renderShadows = true;
        }
    }
}
