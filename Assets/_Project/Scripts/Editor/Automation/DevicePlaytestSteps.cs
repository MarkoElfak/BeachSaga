using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 22 playtest (device check): the busiest beach the game can make. A Resort village with every
    /// place open, every staff slot filled, your character at work and the visitor pool full, played at 1x. Game
    /// scripts make no per-frame allocations, stay well inside a phone's frame budget, and the scene stays within
    /// the draw call budget the M4 test set for phones.
    /// </summary>
    internal static class DevicePlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 600;
        // Game scripts get at most a fifth of a 60 fps frame on this machine, leaving room for slower phone CPUs.
        private const double ScriptBudgetMs = 3.3;
        private const int DrawCallBudget = 1500;
        private static FrameAllocationProbe _probe;

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static StaffService Staff => ServiceLocator.Get<StaffService>();
        private static VisitorService Visitors => ServiceLocator.Get<VisitorService>();
        private static GameDatabase Database => ServiceLocator.Get<GameDatabase>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Built(1);
                case 2: return Built(2);
                case 3: return Built(3);
                case 4: return Built(4);
                case 5: return Built(5);
                case 6: return _probe != null && _probe.Frames >= WarmupFrames;
                case 7: return _probe != null && _probe.Frames >= MeasuredFrames;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: Begin(); break;
                case 1: case 2: case 3: case 4: Check(Buildings.TryUpgrade(0), $"tier {step + 1} started"); break;
                case 5: FillTheResort(); break;
                case 6: _probe.ResetStats(); break; // the first frames after the burst warm pools and caches
                case 7: Report(); break;
                default: Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            ServiceLocator.Get<IEconomyService>().Earn(10000000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void FillTheResort()
        {
            string culture = ServiceLocator.Get<CultureService>().Current.id;
            foreach (ProductDefinition product in Database.products)
            {
                if (product != null && product.IsOpenTo(culture) && Stands.CanUnlock(product)) Stands.TryUnlockProduct(product);
            }

            int open = 0;
            foreach (ProductDefinition product in Database.products) open += product != null && product.IsOpenTo(culture) ? 1 : 0;
            Check(Stands.Count == open, $"every place open to the village is open ({Stands.Count} of {open})");

            HireEveryone();
            Check(Staff.Staff.Count == Staff.Slots, $"every staff slot is filled ({Staff.Staff.Count} of {Staff.Slots})");
            while (Visitors.TrySpawn()) { }
            Check(Visitors.ActiveCount >= 100, $"the visitor pool is full: {Visitors.ActiveCount} visitors");

            Time.timeScale = 1f;
            _probe = FrameAllocationProbe.Create();
        }

        // Fills every free slot with seekers, each at the first place with a free seat.
        private static void HireEveryone()
        {
            for (int guard = 0; Staff.FreeSlots > 0 && guard < 50; guard++)
            {
                Staff.FillSeekers(false);
                if (Staff.Seekers.Count == 0) return;
                int worker = Staff.Seekers[0].Id;
                bool hired = false;
                for (int stand = 0; stand < Stands.Count && !hired; stand++) hired = Staff.CanHire(worker, stand) && Staff.TryHire(worker, stand);
                if (!hired) return;
            }
        }

        private static void Report()
        {
            double scriptMs = _probe.TotalScriptSeconds / _probe.Frames * 1000.0;
            double frameMs = _probe.TotalFrameSeconds / _probe.Frames * 1000.0;
            AutoTestRunner.Write($"Quality level {QualitySettings.names[QualitySettings.GetQualityLevel()]}: {Visitors.ActiveCount} visitors, " +
                                 $"{Staff.Staff.Count} staff, {Stands.Count} places, {_probe.Frames} frames at 1x");
            AutoTestRunner.Write($"Frame avg {frameMs:0.0} ms (vsync), worst {_probe.MaxFrameSeconds * 1000f:0.0} ms; " +
                                 $"game scripts avg {scriptMs:0.00} ms, worst {_probe.MaxScriptSeconds * 1000.0:0.00} ms");
            AutoTestRunner.Write($"Rendering: {UnityStats.drawCalls} draw calls, {UnityStats.setPassCalls} SetPass calls, " +
                                 $"{UnityStats.shadowCasters} shadow casters, {UnityStats.triangles / 1000} k triangles");
            AutoTestRunner.Write(PlaytestTools.RendererBreakdown());
            Check(_probe.FramesWithAllocations == 0,
                $"no per-frame allocations ({_probe.FramesWithAllocations} of {_probe.Frames} frames, worst {_probe.MaxFrameBytes} bytes)");
            Check(scriptMs < ScriptBudgetMs, $"game scripts take {scriptMs:0.00} ms a frame (budget {ScriptBudgetMs} ms)");
            Check(UnityStats.drawCalls < DrawCallBudget, $"{UnityStats.drawCalls} draw calls (budget {DrawCallBudget})");

            Bounds beach = new Bounds(Vector3.zero, Vector3.one);
            foreach (StandView view in Object.FindObjectsByType<StandView>()) beach.Encapsulate(view.transform.position);
            PlaytestTools.FocusCamera(beach.center, 22f + beach.size.x);
            PlaytestTools.CapturePortraitWithHud("m22_1_full_resort.png");
        }

        private static void Finish() => AutoTestRunner.Finish();

        private static bool Built(int tier) => Buildings.MainTier == tier && Buildings.GetLot(0).state == LotState.Built;

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<BuildBarView>() != null;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
