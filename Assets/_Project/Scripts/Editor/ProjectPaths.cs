using System.IO;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>Asset locations used by the setup tools.</summary>
    internal static class ProjectPaths
    {
        public const string Root = "Assets/_Project";
        public const string Materials = Root + "/Art/Materials";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";
        public const string Data = Root + "/ScriptableObjects";
        public const string Config = Data + "/Config";
        public const string Products = Data + "/Products";
        public const string Tiers = Data + "/Tiers";
        public const string Audio = Root + "/Audio";

        public const string BootScene = Scenes + "/Boot.unity";
        public const string BeachScene = Scenes + "/Beach.unity";
        public const string IslandScene = Scenes + "/Island.unity";

        /// <summary>Creates a folder (and its parents) through the AssetDatabase if it is missing.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
