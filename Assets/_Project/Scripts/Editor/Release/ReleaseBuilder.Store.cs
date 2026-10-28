using System.Collections.Generic;
using System.IO;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The Google Play upload: a signed Android App Bundle from a clean cache. It refuses to build while anything
    /// the store needs is missing (StoreReadiness) and raises the version code after each bundle, since Google Play
    /// accepts every version code only once.
    /// </summary>
    internal static partial class ReleaseBuilder
    {
        [MenuItem("Beach Tycoon/Release/Check Google Play Readiness", priority = 63)]
        private static void CheckStoreMenu()
        {
            List<string> problems = StoreProblems();
            if (problems.Count == 0) Debug.Log("[Release] Ready for a Google Play bundle.");
            else Debug.LogWarning("[Release] Before a Google Play bundle:\n- " + string.Join("\n- ", problems));
        }

        [MenuItem("Beach Tycoon/Release/Build Google Play Bundle (AAB)", priority = 64)]
        private static void BuildStoreMenu() => Debug.Log(Build("android-store"));

        public static List<string> StoreProblems()
        {
            ApplySettings();
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProjectPaths.Data + "/GameDatabase.asset");
            string keystore = PlayerSettings.Android.keystoreName;
            bool passwords = !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) && !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass);
            return StoreReadiness.Problems(database != null ? database.monetization : null, PlayerSettings.Android.useCustomKeystore,
                !string.IsNullOrEmpty(keystore) && File.Exists(keystore) && !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName), passwords);
        }

        private static string BuildAndroidStore()
        {
            List<string> problems = StoreProblems();
            if (problems.Count > 0) return "Not built. Before a Google Play bundle:\n- " + string.Join("\n- ", problems) + "\nRESULT: FAIL not ready";

            bool bundle = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = true;
            try
            {
                int code = PlayerSettings.Android.bundleVersionCode;
                string path = $"Builds/Android/BeachTycoon-{Version}-{code}.aab";
                string report = Build("android", BuildOptions.CleanBuildCache, path);
                if (!report.EndsWith("RESULT: built")) return report;

                PlayerSettings.Android.bundleVersionCode = code + 1;
                AssetDatabase.SaveAssets();
                return $"Version {Version} ({code}); the next bundle gets version code {code + 1}\n" + report;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = bundle;
            }
        }
    }
}
