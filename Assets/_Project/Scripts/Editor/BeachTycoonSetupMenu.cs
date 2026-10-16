using System.IO;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// One-click project setup: palette materials, placeholder prefabs, data assets, Boot and Beach scenes,
    /// build scene list and portrait orientation. Safe to run again; it never overwrites tuned data.
    /// </summary>
    internal static class BeachTycoonSetupMenu
    {
        private const string MenuRoot = "Beach Tycoon/";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem(MenuRoot + "Setup Project", priority = 0)]
        private static void SetupProject()
        {
            Run(overwritePlaceholders: false);
        }

        [MenuItem(MenuRoot + "Rebuild Placeholder Prefabs and Scenes", priority = 1)]
        private static void RebuildPlaceholders()
        {
            bool confirmed = EditorUtility.DisplayDialog("Rebuild placeholders",
                "This overwrites the generated prefabs and the Boot and Beach scenes. Data assets are kept.",
                "Rebuild", "Cancel");
            if (confirmed)
            {
                Run(overwritePlaceholders: true);
            }
        }

        [MenuItem(MenuRoot + "Open Boot Scene", priority = 20)]
        private static void OpenBoot()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ProjectPaths.BootScene);
            }
        }

        /// <summary>Runs the full setup. Returns false if it could not run (and says why in the Console).</summary>
        internal static bool Run(bool overwritePlaceholders, bool interactive = true)
        {
            if (!File.Exists(TmpSettingsPath))
            {
                // HUD text needs TMP's default font; import the essentials, then run Setup again.
                AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
                UnityEngine.Debug.LogWarning("Imported TMP Essential Resources. Run Beach Tycoon > Setup Project again.");
                return false;
            }

            bool scenesReady = interactive
                ? EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
                : !HasDirtyScene();
            if (!scenesReady)
            {
                UnityEngine.Debug.LogWarning("Beach Tycoon setup skipped: save or discard open scene changes first.");
                return false;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Beach Tycoon", "Palette and materials", 0.1f);
                ProjectPaths.EnsureFolder(ProjectPaths.Audio);
                ProjectPaths.EnsureFolder(ProjectPaths.Scenes);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // scratch scene
                PaletteDefinition palette = PaletteBuilder.BuildOrSync();
                RenderingAssets.TuneUrpAssets();
                ReportShaderErrors(RenderingAssets.WaterShader);

                EditorUtility.DisplayProgressBar("Beach Tycoon", "Placeholder prefabs", 0.3f);
                PlaceholderPrefabBuilder.BuildAll(palette, overwritePlaceholders);

                EditorUtility.DisplayProgressBar("Beach Tycoon", "Data assets", 0.5f);
                DataAssetBuilder.BuildOrUpdate(palette);

                EditorUtility.DisplayProgressBar("Beach Tycoon", "Scenes", 0.7f);
                if (overwritePlaceholders || !SceneExists(ProjectPaths.BootScene))
                {
                    SceneBuilder.BuildBootScene();
                }

                if (overwritePlaceholders || !SceneExists(ProjectPaths.BeachScene))
                {
                    SceneBuilder.BuildBeachScene();
                }

                if (overwritePlaceholders || !SceneExists(ProjectPaths.IslandScene))
                {
                    SceneBuilder.BuildIslandScene();
                }

                SceneBuilder.RegisterBuildScenes();
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(ProjectPaths.BootScene);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            UnityEngine.Debug.Log("Beach Tycoon setup complete. Press Play in the Boot scene.");
            return true;
        }

        private static void ReportShaderErrors(string shaderName)
        {
            Shader shader = UnityEngine.Shader.Find(shaderName);
            if (shader == null)
            {
                UnityEngine.Debug.LogError($"Shader '{shaderName}' was not found.");
                return;
            }

            foreach (var message in ShaderUtil.GetShaderMessages(shader))
            {
                UnityEngine.Debug.LogError($"{shaderName} line {message.line}: {message.message}");
            }
        }

        private static bool HasDirtyScene()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                // Unsaved untitled scenes (e.g. a scratch scene from an earlier run) are safe to discard.
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isDirty && !string.IsNullOrEmpty(scene.path))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SceneExists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
        }
    }
}
