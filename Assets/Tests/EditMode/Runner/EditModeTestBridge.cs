using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Runs the EditMode tests from a menu item, or when Logs/BeachTycoonTests.request appears (automation),
    /// and writes every run's results (also runs started from the Test Runner window) to
    /// Logs/BeachTycoonTests.log. A request file may name a playtest suite to start once the tests pass.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditModeTestBridge
    {
        private const string RequestPath = "Logs/BeachTycoonTests.request";
        private const string ReportPath = "Logs/BeachTycoonTests.log";
        private const string PlaytestRequestPath = "Temp/BeachTycoonAutoRun.request";
        private const string AssemblyName = "BeachTycoon.Tests.EditMode";
        private const string FollowUpKey = "BeachTycoon.Tests.FollowUp";
        private const string PendingPath = "Logs/BeachTycoonTests.followup";

        // The test runner still restores the scene setup for a moment after RunFinished; entering Play mode
        // during that throws, so the follow-up playtest starts a little later.
        private const double FollowUpDelaySeconds = 3.0;

        static EditModeTestBridge()
        {
            TestRunnerApi.RegisterTestCallback(new Reporter());
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        [MenuItem("Beach Tycoon/Run EditMode Tests", priority = 30)]
        private static void RunFromMenu() => Run(string.Empty);

        private static void Poll()
        {
            StartPendingPlaytest();
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string followUp = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            Run(followUp);
        }

        private static void StartPendingPlaytest()
        {
            if (!File.Exists(PendingPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode
                || (System.DateTime.UtcNow - File.GetLastWriteTimeUtc(PendingPath)).TotalSeconds < FollowUpDelaySeconds)
            {
                return;
            }

            File.Copy(PendingPath, PlaytestRequestPath, true);
            File.Delete(PendingPath);
        }

        private static void Run(string followUpSuite)
        {
            SessionState.SetString(FollowUpKey, followUpSuite);
            File.WriteAllText(ReportPath, "EditMode tests running...\n");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { AssemblyName } };
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class Reporter : ICallbacks
        {
            private readonly StringBuilder _failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) => _failures.Clear();

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren || result.TestStatus != TestStatus.Failed) return;
                _failures.Append("FAIL ").Append(result.FullName).Append('\n')
                    .Append("    ").Append(result.Message?.Trim().Replace("\n", "\n    ")).Append('\n');
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var report = new StringBuilder();
                report.Append($"EditMode tests {System.DateTime.UtcNow:u}\n");
                report.Append(_failures);
                report.Append($"passed {result.PassCount}, failed {result.FailCount}, skipped {result.SkipCount}, " +
                              $"inconclusive {result.InconclusiveCount}, {result.Duration:0.00} s\n");
                report.Append(result.FailCount == 0 ? "RESULT: PASS\n" : "RESULT: FAIL\n");
                File.WriteAllText(ReportPath, report.ToString());

                string followUp = SessionState.GetString(FollowUpKey, string.Empty);
                SessionState.EraseString(FollowUpKey);
                if (result.FailCount > 0 || followUp.Length == 0) return;
                File.WriteAllText(PendingPath, followUp);
            }
        }
    }
}
