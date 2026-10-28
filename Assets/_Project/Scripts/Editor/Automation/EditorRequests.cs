using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Editor jobs requested through a file, so they can be started from outside the Editor. Write one command
    /// to Temp/BeachTycoonEditor.request; the result goes to Logs/BeachTycoonEditor.log.
    ///   probe                   build targets, identity and packages
    ///   packages add a b ...    adds packages (latest compatible version) through the Package Manager
    ///   build android|ios|mac   see ReleaseBuilder
    ///   shot file.png           runs the Mac build at phone quality; it saves a screenshot of the beach and quits
    ///   economy [id=price ...]  writes the economy balance table (EconomyReport); with prices, a what-if to Logs
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorRequests
    {
        private const string RequestPath = "Temp/BeachTycoonEditor.request";
        internal const string LogPath = "Logs/BeachTycoonEditor.log";

        private static AddAndRemoveRequest _adding;

        static EditorRequests()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (_adding != null)
            {
                if (_adding.IsCompleted) FinishAdding();
                return;
            }

            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string command = File.ReadAllText(RequestPath).Trim();
            if (command.Length == 0) return; // seen before its text arrived; read it again next update
            File.Delete(RequestPath);
            string[] words = command.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return;

            if (words[0] == "probe") Write(Probe());
            else if (words[0] == "packages" && words.Length > 2 && words[1] == "add") StartAdding(words);
            else if (words[0] == "build" && words.Length > 1) Write(ReleaseBuilder.Build(words[1]));
            else if (words[0] == "shot" && words.Length > 1) Write(LaunchShot(words[1]));
            else if (words[0] == "economy") Write(EconomyReport.WriteRequest(words));
            else Write("RESULT: unknown command '" + command + "'");
        }

        private static string Probe()
        {
            var text = new StringBuilder("Probe " + System.DateTime.UtcNow.ToString("u") + "\n");
            text.AppendLine("Unity " + Application.unityVersion + ", active target " + EditorUserBuildSettings.activeBuildTarget);
            text.AppendLine("Android support: " + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android));
            text.AppendLine("iOS support: " + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS));
            text.AppendLine("Product " + PlayerSettings.productName + ", company " + PlayerSettings.companyName +
                ", id " + PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android) + ", version " + PlayerSettings.bundleVersion);
            text.AppendLine("Cloud project linked: " + !string.IsNullOrEmpty(CloudProjectSettings.projectId));
            foreach (UnityEditor.PackageManager.PackageInfo package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (package.isDirectDependency && !package.name.StartsWith("com.unity.modules")) text.AppendLine("  " + package.name + " " + package.version);
            }

            text.Append("RESULT: done");
            return text.ToString();
        }

        private static string LaunchShot(string file)
        {
            string folder = Path.Combine(ReleaseBuilder.MacAppPath, "Contents/MacOS");
            if (!Directory.Exists(folder)) return "RESULT: FAIL build mac first";
            string executable = Directory.GetFiles(folder)[0];
            string target = Path.GetFullPath(file);
            File.Delete(target);
            System.Diagnostics.Process.Start(executable,
                $"-screen-fullscreen 0 -screen-width 540 -screen-height 960 -logFile \"{target}.log\" -beachQuality 0 -beachShot \"{target}\"");
            return "Started " + executable + "\nRESULT: launched";
        }

        private static void StartAdding(string[] words)
        {
            string[] names = new string[words.Length - 2];
            System.Array.Copy(words, 2, names, 0, names.Length);
            Write("Adding " + string.Join(", ", names) + " ...");
            _adding = Client.AddAndRemove(names);
        }

        private static void FinishAdding()
        {
            var text = new StringBuilder();
            if (_adding.Status == StatusCode.Success)
            {
                foreach (UnityEditor.PackageManager.PackageInfo package in _adding.Result) text.AppendLine("  " + package.name + " " + package.version);
                text.Append("RESULT: added");
            }
            else
            {
                text.Append("RESULT: FAIL " + _adding.Error?.message);
            }

            _adding = null;
            Write(text.ToString());
        }

        internal static void Write(string text)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, text + "\n");
        }
    }
}
