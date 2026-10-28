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
    /// Milestone 18 playtest (hiring visitors): the tent has no staff slots; the cabin brings three job seekers with a
    /// badge. Tapping one opens their card with stars per skill; hiring them for the towel stand (half speed while
    /// empty) sends them over, and it serves faster. The staff list and the stand panel show them, a missed payday
    /// slows them down until they are paid, and after a restart they are still at work.
    /// </summary>
    internal static class StaffPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const string Key = "BeachTycoon.AutoRun.Staff.";

        private static StaffService Staff => ServiceLocator.Get<StaffService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static WorkerViewLayer Views => PlaytestTools.Find<WorkerViewLayer>();
        private static WorkerCardView Card => PlaytestTools.Find<WorkerCardView>();
        private static StaffPanelView List => PlaytestTools.Find<StaffPanelView>();
        private static StandController Towels => Stands.FindByProduct("towels");
        private static StaffMember Worker => Towels != null ? Staff.WorkerAt(Towels.Index) : null;

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Buildings.GetLot(0).state == LotState.Built && ServiceLocator.Get<CharacterService>().IsWorking;
                case 2: return BeachReady() && Buildings.MainTier == 2 && Buildings.GetLot(0).state == LotState.Built && SeekersArrived();
                case 3: return Worker != null && Worker.IsWorking;
                case 4: return BeachReady() && Staff.Staff.Count == 1 && Views != null && Views.Count == Staff.Staff.Count + Staff.Seekers.Count;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: Begin(); break;
                case 1: Upgrade(); break;
                case 2: HireFromCard(); break;
                case 3: CheckAtWork(); break;
                case 4: CheckAfterRestart(); break;
                default: AutoTestRunner.Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            screen.InputText = "Staff Bay";
            screen.Picker.Card(1).Button.onClick.Invoke();
            PlaytestTools.Click(screen, "ConfirmButton");
            Economy.Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void Upgrade()
        {
            Check(Staff.Slots == 0 && Staff.Seekers.Count == 0 && !List.ShowsButton, "the tent has no staff slots and nobody applies");
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void HireFromCard()
        {
            Check(Staff.Slots == 2 && Staff.Seekers.Count == Staff.Config.maxJobSeekers, $"the cabin allows 2 staff; {Staff.Seekers.Count} job seekers came");
            bool badges = true;
            foreach (StaffMember seeker in Staff.Seekers) badges &= Views.Find(seeker.Id) != null && Views.Find(seeker.Id).ShowsBadge;
            Check(badges, "every job seeker shows a badge");
            Check(List.ShowsButton && List.ShowsBadge && List.ButtonText == "Staff 0/2", $"the HUD shows '{List.ButtonText}' with a badge");
            Check(Stands.TryUnlockProduct(ServiceLocator.Get<GameDatabase>().FindProduct("towels")), "towel stand opened");
            Check(Mathf.Approximately(Towels.ServeSpeed, Staff.Config.unstaffedServeSpeed), $"with nobody there it serves at half speed ({Towels.ServeSpeed:0.00})");

            StaffMember first = Staff.Seekers[0];
            Vector3 at = first.Agent.Position;
            PlaytestTools.FocusCamera(at, 13f);
            PlaytestTools.CapturePortraitWithHud("m18_1_seekers.png");
            PlaytestTools.TapWorld(at + Vector3.up * 0.9f);
            Check(Card.IsOpen && Card.WorkerId == first.Id, $"tapping a job seeker opens their card ({PlaytestTools.HitName(at + Vector3.up * 0.9f)})");
            Check(Card.NameText == first.Model.name && Card.StatusText == "Looking for a job", $"the card shows {Card.NameText}: '{Card.StatusText}'");
            PlaytestTools.CapturePortraitWithHud("m18_2_card.png");

            Card.StandButton(Towels.Index).onClick.Invoke();
            Check(!Card.IsOpen && Worker == first && !first.IsSeeker && first.IsWalking, $"{first.Model.name} is hired and walks to the towel stand");
            Check(!Staff.CanHire(Staff.Seekers[0].Id, 0), "the character's stand can't be given away");
        }

        private static void CheckAtWork()
        {
            StaffMember worker = Worker;
            float speed = StaffMath.ServeSpeed(Staff.Config, worker.Model, StaffSkill.Selling, Staff.IsFromHome(worker.Model));
            Check(Mathf.Approximately(Towels.ServeSpeed, speed) && speed >= 1f, $"at work, the towel stand serves at {speed:0.00}x");
            Check(Views.Find(worker.Id) != null && !Views.Find(worker.Id).ShowsBadge, "a hired worker has no badge");
            PlaytestTools.FocusCamera(worker.Agent.Position, 8f);
            PlaytestTools.CapturePortrait("m18_3_at_work.png");

            PlaytestTools.Click(List.transform.parent, "StaffButton");
            Check(List.IsOpen && List.Row(0).Text.Contains(worker.Model.name) && List.SummaryText.Contains("1 of 2"),
                $"the staff list shows {worker.Model.name}: '{List.SummaryText}'");
            PlaytestTools.CapturePortraitWithHud("m18_4_staff_list.png");
            List.Close();

            Vector3 stand = StandPosition(Towels.Index);
            PlaytestTools.TapAndUpgrade(stand + Vector3.up * 0.8f);
            UpgradePanelView panel = PlaytestTools.Find<UpgradePanelView>();
            Check(panel.IsOpen && panel.BodyText.Contains(worker.Model.name) && panel.HireText.Contains(worker.Model.name),
                $"the stand panel names who works there ('{panel.HireText}')");
            PlaytestTools.CapturePortraitWithHud("m18_5_stand_panel.png");
            panel.Close();

            CheckPayday(worker, speed);
            ServiceLocator.Get<ISaveService>().Save();
            SessionState.SetString(Key + "Name", worker.Model.name);
            AutoTestRunner.RestartPlayMode();
        }

        // Paid: coins go down by the wage. Unpaid: slower and the card says so, until the next wage is paid.
        private static void CheckPayday(StaffMember worker, float speed)
        {
            long wage = Staff.Wage(worker.Model);
            long before = Economy.Coins;
            Staff.PayWages();
            Check(Economy.Coins == before - wage && worker.Model.missedPays == 0, $"payday pays {wage} coins");

            Economy.TrySpend(Economy.Coins);
            Staff.PayWages();
            Check(worker.Model.missedPays == 1 && Towels.ServeSpeed < speed, $"an unpaid worker slows down ({Towels.ServeSpeed:0.00}x)");
            EventBus.Publish(new WorkerCardRequested(worker.Id));
            Check(Card.StatusText.StartsWith("Unpaid"), $"their card says '{Card.StatusText}'");
            Card.Close();
            Economy.Earn(500000);
            Staff.PayWages();
            Check(worker.Model.missedPays == 0 && Mathf.Approximately(Towels.ServeSpeed, speed), "paid again, they are back to full speed");
        }

        private static void CheckAfterRestart()
        {
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            if (screen != null && screen.IsOpen) screen.Close();
            StaffMember worker = Worker;
            Check(worker != null && worker.IsWorking && worker.Model.name == SessionState.GetString(Key + "Name", "?"),
                "after a restart the same worker is already at the towel stand");
            Check(Staff.Seekers.Count == Staff.Config.maxJobSeekers - 1, $"{Staff.Seekers.Count} job seekers are still waiting");
            Check(ServiceLocator.Get<ISaveService>().Data.version == GameSaveData.CurrentVersion, "saved as version " + GameSaveData.CurrentVersion);
        }

        private static bool SeekersArrived()
        {
            if (Staff.Seekers.Count == 0 || Views == null) return false;
            foreach (StaffMember seeker in Staff.Seekers) if (seeker.IsWalking) return false;
            return true;
        }

        private static Vector3 StandPosition(int index)
        {
            ServiceLocator.Get<WorldNavigation>().TryGetStandPosition(index, out Vector3 position);
            return position;
        }

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<BuildBarView>() != null;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
