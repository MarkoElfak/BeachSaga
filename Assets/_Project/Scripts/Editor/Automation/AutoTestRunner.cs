using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// One-shot automated playtest. When Temp/BeachTycoonAutoRun.request exists (or from the menu) it deletes the
    /// save, regenerates placeholders, enters Play mode and runs PlaytestSteps, which can restart Play mode to
    /// test save/load. Writes Logs/BeachTycoonAutoRun.log and screenshots. State survives domain reloads.
    /// </summary>
    [InitializeOnLoad]
    internal static class AutoTestRunner
    {
        private const string RequestPath = "Temp/BeachTycoonAutoRun.request";
        private const string ReportPath = "Logs/BeachTycoonAutoRun.log";
        private const string KeyPrefix = "BeachTycoon.AutoRun.";
        private const double BootTimeoutSeconds = 30.0;
        private const double StepMinDelaySeconds = 0.5;
        private const double StepTimeoutSeconds = 45.0;

        private enum Stage { Idle, WaitingForPlay, Playing, Restarting, Cleanup }

        private static double _stageStartTime;

        static AutoTestRunner()
        {
            _stageStartTime = Now;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static Stage CurrentStage
        {
            get => (Stage)SessionState.GetInt(KeyPrefix + "Stage", 0);
            set => SessionState.SetInt(KeyPrefix + "Stage", (int)value);
        }

        private static int CurrentStep
        {
            get => SessionState.GetInt(KeyPrefix + "Step", 0);
            set => SessionState.SetInt(KeyPrefix + "Step", value);
        }

        private static double Now => EditorApplication.timeSinceStartup;

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveService.FileName);

        [MenuItem("Beach Tycoon/Run Automated Playtest", priority = 40)]
        private static void RequestFromMenu() => File.WriteAllText(RequestPath, "menu");

        public static void Write(string line)
        {
            string report = SessionState.GetString(KeyPrefix + "Report", string.Empty) + line + "\n";
            SessionState.SetString(KeyPrefix + "Report", report);
            File.WriteAllText(ReportPath, report);
        }

        public static void Fail(string reason)
        {
            SessionState.SetInt(KeyPrefix + "Failures", SessionState.GetInt(KeyPrefix + "Failures", 0) + 1);
            Write("FAIL: " + reason);
        }

        public static void RestartPlayMode()
        {
            Write("Restarting Play mode to test save/load");
            CurrentStage = Stage.Restarting;
            EditorApplication.ExitPlaymode();
        }

        public static void Finish()
        {
            int failures = SessionState.GetInt(KeyPrefix + "Failures", 0);
            Write(failures == 0 ? "RESULT: PASS" : $"RESULT: {failures} check(s) failed");
            Time.timeScale = 1f;
            CurrentStage = Stage.Cleanup;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void Update()
        {
            switch (CurrentStage)
            {
                case Stage.Idle:
                    if (File.Exists(RequestPath) && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                        && !EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        File.Delete(RequestPath);
                        Begin();
                    }
                    break;
                case Stage.WaitingForPlay:
                    WaitForBoot();
                    break;
                case Stage.Playing:
                    TickSteps();
                    break;
                case Stage.Restarting:
                    if (!EditorApplication.isPlayingOrWillChangePlaymode) EnterPlay();
                    break;
                case Stage.Cleanup:
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        File.Delete(SavePath); // leave the user a fresh game
                        CurrentStage = Stage.Idle;
                    }
                    break;
            }
        }

        private static void Begin()
        {
            SessionState.SetString(KeyPrefix + "Report", string.Empty);
            SessionState.SetInt(KeyPrefix + "Failures", 0);
            CurrentStep = 0;
            CurrentStage = Stage.WaitingForPlay; // marks the run active so setup logs are captured
            Write("Automated playtest started " + System.DateTime.UtcNow.ToString("u"));
            File.Delete(SavePath);

            bool setupOk;
            try
            {
                setupOk = BeachTycoonSetupMenu.Run(overwritePlaceholders: true, interactive: false);
            }
            catch (System.Exception exception)
            {
                Write("[Exception] " + exception);
                setupOk = false;
            }

            if (!setupOk)
            {
                Fail("setup did not complete");
                Finish();
                return;
            }

            Write("Setup complete");
            EnterPlay();
        }

        private static void EnterPlay()
        {
            CurrentStage = Stage.WaitingForPlay;
            _stageStartTime = Now;
            EditorApplication.EnterPlaymode();
        }

        private static void WaitForBoot()
        {
            if (!EditorApplication.isPlaying) return;
            Application.runInBackground = true; // test only: keep ticking when Unity is not in front

            if (GameBootstrap.IsReady && Camera.main != null && SceneManager.GetActiveScene().name == SceneNames.Beach)
            {
                Write($"Beach scene running (step {CurrentStep})");
                CurrentStage = Stage.Playing;
                _stageStartTime = Now;
            }
            else if (Now - _stageStartTime > BootTimeoutSeconds)
            {
                Fail("Beach scene did not start");
                Finish();
            }
        }

        private static void TickSteps()
        {
            if (!EditorApplication.isPlaying)
            {
                Fail("Play mode was stopped");
                Finish();
                return;
            }

            double waited = Now - _stageStartTime;
            int step = CurrentStep;
            bool timedOut = waited >= StepTimeoutSeconds;
            if (waited < StepMinDelaySeconds || (!PlaytestSteps.IsReady(step) && !timedOut)) return;

            if (timedOut) Fail($"step {step} timed out waiting for its condition");
            _stageStartTime = Now;
            CurrentStep = step + 1;
            PlaytestSteps.Run(step);
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (CurrentStage != Stage.Idle && type != LogType.Log && !message.StartsWith("Account API"))
            {
                Write($"[{type}] {message}");
            }
        }
    }
}
