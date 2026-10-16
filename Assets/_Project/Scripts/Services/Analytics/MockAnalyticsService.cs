using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>No-op analytics: remembers recent events (for tests and debugging) and can echo them to the Console.</summary>
    public sealed class MockAnalyticsService : IAnalyticsService
    {
        private const int HistorySize = 64;

        private readonly Queue<string> _recent = new Queue<string>(HistorySize);
        private readonly bool _logToConsole;

        public MockAnalyticsService(bool logToConsole)
        {
            _logToConsole = logToConsole;
        }

        public IReadOnlyCollection<string> Recent => _recent;

        public void LogEvent(string eventName) => Record(eventName);

        public void LogEvent(string eventName, string parameter, long value) =>
            Record(eventName + " " + parameter + "=" + value);

        public void LogEvent(string eventName, string parameter, string value) =>
            Record(eventName + " " + parameter + "=" + value);

        public bool HasLogged(string eventName)
        {
            foreach (string entry in _recent)
            {
                if (entry == eventName || entry.StartsWith(eventName + " ")) return true;
            }

            return false;
        }

        // Events are rare (upgrades, sessions), so the string work here is not a per-frame cost.
        private void Record(string entry)
        {
            if (_recent.Count == HistorySize) _recent.Dequeue();
            _recent.Enqueue(entry);
            if (_logToConsole) Debug.Log("[Analytics] " + entry);
        }
    }
}
