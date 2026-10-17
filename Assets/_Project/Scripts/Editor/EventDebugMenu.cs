using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Play mode shortcuts for trying beach events and the time of day without waiting.</summary>
    internal static class EventDebugMenu
    {
        private const string Root = "Beach Tycoon/Debug/";

        [MenuItem(Root + "Start Next Beach Event", priority = 80)]
        private static void StartNext()
        {
            BeachEventService events = ServiceLocator.Get<BeachEventService>();
            BeachEventDefinition[] all = ServiceLocator.Get<GameDatabase>().beachEvents.events;
            int current = System.Array.IndexOf(all, events.Active);
            for (int step = 1; step <= all.Length; step++)
            {
                BeachEventDefinition next = all[(current + step + all.Length) % all.Length];
                if (events.Begin(next)) return;
            }

            Debug.Log("No beach event can run yet (each needs its tier and stand).");
        }

        [MenuItem(Root + "End Beach Event", priority = 81)]
        private static void End() => ServiceLocator.Get<BeachEventService>().End();

        [MenuItem(Root + "Skip Quarter Day", priority = 82)]
        private static void SkipQuarterDay()
        {
            DayClock clock = ServiceLocator.Get<DayClock>();
            clock.SetPhase(clock.Phase + 0.25f);
        }

        [MenuItem(Root + "Start Next Beach Event", true)]
        [MenuItem(Root + "End Beach Event", true)]
        [MenuItem(Root + "Skip Quarter Day", true)]
        private static bool IsPlaying() => Application.isPlaying && ServiceLocator.TryGet(out DayClock _);
    }
}
