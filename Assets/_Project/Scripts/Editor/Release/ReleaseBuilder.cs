using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Store-ready player settings (name, app id, version, portrait, IL2CPP ARM64, icon, splash) and one-click Android
    /// and iOS builds into Builds/. Settings are applied before every build, so a build never ships stale values.
    /// The signed Google Play bundle is in ReleaseBuilder.Store.
    /// </summary>
    internal static partial class ReleaseBuilder
    {
        public const string ProductName = "Beach Tycoon";
        public const string CompanyName = "Marko Ilic";
        public const string AppId = "com.markoelfak.beachtycoon";
        /// <summary>The version players see in the store; raise it for each release.</summary>
        public const string Version = "1.0.0";
        private const string IconFolder = ProjectPaths.Root + "/Art/Icon";
        private const string IconPath = IconFolder + "/AppIcon.png";
        private const string SplashPath = IconFolder + "/SplashLogo.png";
        private static readonly Color SplashBackground = new Color(0.36f, 0.72f, 0.98f);

        [MenuItem("Beach Tycoon/Release/Apply Store Settings", priority = 60)]
        public static void ApplySettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = true; // ads, purchases and analytics
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber)) PlayerSettings.iOS.buildNumber = "1";

            ApplyIconAndSplash();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Beach Tycoon/Release/Build Android (APK)", priority = 61)]
        private static void BuildAndroidMenu() => Debug.Log(Build("android"));

        [MenuItem("Beach Tycoon/Release/Build iOS (Xcode project)", priority = 62)]
        private static void BuildIosMenu() => Debug.Log(Build("ios"));

        /// <summary>A desktop copy of the game, used to check how a built player renders.</summary>
        public const string MacAppPath = "Builds/Mac/BeachTycoon.app";

        /// <summary>
        /// Builds "android" (Builds/Android/BeachTycoon.apk), "android-store" (the signed Google Play bundle), "ios"
        /// (Builds/iOS) or "mac"; returns a short report.
        /// Phone test builds: "android-dev" (Vulkan) and "android-gles" (OpenGL ES 3 only) are development builds
        /// that show errors on screen, written next to the release APK.
        /// </summary>
        public static string Build(string platform)
        {
            if (platform == "android-dev" || platform == "android-gles") return BuildAndroidTest(platform == "android-gles");
            if (platform == "android-store") return BuildAndroidStore();
            // Store builds are always clean: an incremental Android build once shipped without the 3D world.
            BuildOptions options = platform == "mac" ? BuildOptions.None : BuildOptions.CleanBuildCache;
            return Build(platform, options, null);
        }

        private static string BuildAndroidTest(bool gles)
        {
            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            bool automatic = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            if (gles)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            }

            try
            {
                string name = gles ? "BeachTycoon-gles-test.apk" : "BeachTycoon-vulkan-test.apk";
                // A clean build, so a switch of graphics API is never served from the previous build's cache.
                return Build("android", BuildOptions.Development | BuildOptions.CleanBuildCache, "Builds/Android/" + name);
            }
            finally
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, automatic);
                if (!automatic) PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
                AssetDatabase.SaveAssets();
            }
        }

        private static string Build(string platform, BuildOptions buildOptions, string pathOverride)
        {
            bool android = platform == "android", mac = platform == "mac";
            if (!android && !mac && platform != "ios") return "RESULT: FAIL unknown platform " + platform;
            BuildTarget target = android ? BuildTarget.Android : mac ? BuildTarget.StandaloneOSX : BuildTarget.iOS;
            BuildTargetGroup group = android ? BuildTargetGroup.Android : mac ? BuildTargetGroup.Standalone : BuildTargetGroup.iOS;
            if (!BuildPipeline.IsBuildTargetSupported(group, target)) return $"RESULT: FAIL {target} build support is not installed (add the module in Unity Hub)";

            ApplySettings();
            string path = pathOverride ?? (android ? "Builds/Android/BeachTycoon.apk" : mac ? MacAppPath : "Builds/iOS");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Builds");
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = buildOptions,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string size = android && File.Exists(path) ? $", {new FileInfo(path).Length / (1024f * 1024f):0.0} MB" : string.Empty;
            return $"{target} build {summary.result} in {summary.totalTime.TotalSeconds:0} s, {summary.totalErrors} errors, " +
                $"{summary.totalWarnings} warnings{size} -> {path}\n{Errors(report)}RESULT: {(summary.result == BuildResult.Succeeded ? "built" : "FAIL")}";
        }

        // The first few build errors, so a failed build says why without opening the Editor log.
        private static string Errors(BuildReport report)
        {
            var text = new System.Text.StringBuilder();
            int count = 0;
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type != LogType.Error && message.type != LogType.Exception) continue;
                    if (++count > 10) return text.ToString();
                    text.AppendLine($"error ({step.name}): {message.content}");
                }
            }

            return text.ToString();
        }

        // Paints the icon and splash logo once, then points the player settings at them.
        private static void ApplyIconAndSplash()
        {
            ProjectPaths.EnsureFolder(IconFolder);
            Texture2D icon = SavePainting(IconPath, 1024, false, TextureImporterType.Default);
            Texture2D logo = SavePainting(SplashPath, 512, true, TextureImporterType.Sprite);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
            if (sprite != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, sprite) };
            if (logo == null) Debug.LogWarning("Splash logo could not be created.");
        }

        private static Texture2D SavePainting(string path, int size, bool rounded, TextureImporterType type)
        {
            if (!File.Exists(path))
            {
                Texture2D painting = AppIconPainter.Paint(size, rounded);
                File.WriteAllBytes(path, painting.EncodeToPNG());
                Object.DestroyImmediate(painting);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && (importer.textureType != type || importer.mipmapEnabled || !importer.alphaIsTransparency))
            {
                importer.textureType = type;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
