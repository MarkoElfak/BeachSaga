using System.IO;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Imports changed files when Logs/BeachTycoonRefresh.request appears, so scripts copied in by automation
    /// compile while the Editor is in the background (it otherwise only refreshes on focus). Lives in the
    /// Diagnostics assembly so it keeps working while gameplay assemblies have compile errors.
    /// </summary>
    [InitializeOnLoad]
    internal static class AutomationRefresh
    {
        private const string RequestPath = "Logs/BeachTycoonRefresh.request";

        static AutomationRefresh()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            File.Delete(RequestPath);
            AssetDatabase.Refresh();
        }
    }
}
