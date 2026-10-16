using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Creates the terrain, water and scatter materials, the post-processing profile, and tunes the
    /// project's URP assets (softer, longer shadows on mobile too).
    /// </summary>
    internal static class RenderingAssets
    {
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string WaterShader = "BeachTycoon/Water";
        private const string Folder = ProjectPaths.Root + "/Art/Rendering";
        private const string ProfilePath = Folder + "/BeachVolumeProfile.asset";

        public static Material TerrainMaterial()
        {
            Material material = LoadOrCreate("M_Terrain", LitShader);
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", TextureFactory.FlatNormal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            // Smoothness comes from the baked albedo alpha (wet sand is glossy, dry sand is matte).
            material.SetFloat("_SmoothnessTextureChannel", 1f);
            material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_EnvironmentReflections", 1f);
            return Finish(material);
        }

        public static Material WaterMaterial()
        {
            Material material = LoadOrCreate("M_Water", WaterShader);
            material.SetTexture("_NoiseTex", TextureFactory.Noise);
            material.renderQueue = (int)RenderQueue.Transparent;
            return Finish(material);
        }

        public static Material RockMaterial()
        {
            Material material = LoadOrCreate("M_Rock", LitShader);
            material.SetColor("_BaseColor", new Color(0.56f, 0.53f, 0.5f));
            material.SetTexture("_BaseMap", TextureFactory.Plaster);
            material.SetFloat("_Smoothness", 0.18f);
            return Finish(material);
        }

        public static Material GrassTuftMaterial()
        {
            Material material = LoadOrCreate("M_GrassTuft", LitShader);
            material.SetColor("_BaseColor", new Color(0.42f, 0.62f, 0.3f));
            material.SetFloat("_Smoothness", 0.05f);
            SetDoubleSided(material);
            return Finish(material);
        }

        public static void SetDoubleSided(Material material)
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
        }

        public static VolumeProfile PostProcessingProfile()
        {
            ProjectPaths.EnsureFolder(Folder);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var tonemapping = Add<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.35f);
            color.contrast.Override(12f);
            color.saturation.Override(14f);
            var white = Add<WhiteBalance>(profile);
            white.temperature.Override(8f);
            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Longer, softer shadows (with two cascades) on every URP asset in the project.</summary>
        public static void TuneUrpAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(asset);
                SetFloat(serialized, "m_ShadowDistance", 70f);
                SetInt(serialized, "m_ShadowCascadeCount", 2);
                SetInt(serialized, "m_SoftShadowsSupported", 1);
                SetInt(serialized, "m_MainLightShadowmapResolution", 2048);
                SetInt(serialized, "m_SupportsHDR", 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static Material LoadOrCreate(string name, string shaderName)
        {
            ProjectPaths.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader '{shaderName}' not found.");
                shader = Shader.Find(LitShader);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static Material Finish(Material material)
        {
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) return;
            if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = value != 0;
            else property.intValue = value;
        }
    }
}
