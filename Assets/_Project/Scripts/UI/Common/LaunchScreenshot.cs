using System.Collections;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Build check: started with "-beachShot &lt;file&gt;" (and optionally "-beachQuality &lt;level&gt;", e.g. 0 for the
    /// phone settings), the player closes the opening windows, saves one screenshot of the beach and quits, so a
    /// built game can be compared with the Editor. Does nothing otherwise.
    /// </summary>
    public sealed class LaunchScreenshot : MonoBehaviour
    {
        private const float SettleSeconds = 10f;

        private string _path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-beachQuality" && int.TryParse(args[i + 1], out int level)) QualitySettings.SetQualityLevel(level, true);
                if (args[i] != "-beachShot") continue;
                Application.runInBackground = true; // the window may open behind the Editor
                var host = new GameObject("LaunchScreenshot");
                DontDestroyOnLoad(host);
                host.AddComponent<LaunchScreenshot>()._path = args[i + 1];
            }
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(SettleSeconds);
            foreach (PrivacyPanelView privacy in FindObjectsByType<PrivacyPanelView>()) privacy.Close();
            foreach (StartScreenView start in FindObjectsByType<StartScreenView>()) start.Close();
            yield return new WaitForSecondsRealtime(1f);
            ScreenCapture.CaptureScreenshot(_path);
            yield return new WaitForSecondsRealtime(2f);
            Application.Quit();
        }
    }
}
