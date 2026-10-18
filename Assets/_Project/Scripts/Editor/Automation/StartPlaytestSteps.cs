using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 13 playtest (start screen): a new game asks for the village name and a culture and cannot skip
    /// them; bad names are refused with a message, the Idea button suggests one, and the name lands in the HUD, on
    /// the welcome sign and in the save. The next start greets the player with the name and a Play button (once per app
    /// start), Settings renames the village while the culture stays, and a visit shows the name on the island.
    /// </summary>
    internal static class StartPlaytestSteps
    {
        private const string FirstName = "Sunny Bay";
        private const string NewName = "Coral Cove";

        private static VillageService Village => ServiceLocator.Get<VillageService>();
        private static VisitService Visits => ServiceLocator.Get<VisitService>();
        private static StartScreenView Screen => PlaytestTools.Find<StartScreenView>();
        private static string HudName => PlaytestTools.Find<VillageNameView>().Text;
        private static string SignName => PlaytestTools.Find<VillageSignView>().Text;

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Screen != null && Screen.IsOpen;
                case 2: return SceneManager.GetActiveScene().name == SceneNames.Island && PlaytestTools.Find<IslandHudView>() != null;
                case 3: return BeachReady();
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: NameTheVillage(); break;
                case 1: GreetAndRename(); break;
                case 2: CheckVisit(); break;
                case 3: CheckBackHome(); break;
                default: AutoTestRunner.Finish(); break;
            }
        }

        private static void NameTheVillage()
        {
            StartScreenView screen = Screen;
            Check(!Village.IsFounded && screen.IsNamePage, "a new game opens the start screen on the name page");
            Check(!screen.CanClose && screen.HeadingText == "Found your village", "it has no close button: the village must be founded");
            PlaytestTools.CapturePortraitWithHud("m13_1_name.png");

            screen.InputText = "A";
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(screen.IsOpen && screen.ErrorText == "Use at least 2 characters.", $"a one-letter name is refused: '{screen.ErrorText}'");
            screen.InputText = "Bay <b>";
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(screen.IsOpen && screen.ErrorText.StartsWith("Use letters"), $"odd characters are refused: '{screen.ErrorText}'");

            PlaytestTools.Click(screen, "IdeaButton");
            string idea = screen.InputText;
            Check(System.Array.IndexOf(ServiceLocator.Get<GameDatabase>().village.suggestedNames, idea) >= 0 && screen.ErrorText.Length == 0,
                $"Idea suggests '{idea}' and clears the message");

            screen.InputText = "  Sunny    Bay ";
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(screen.IsOpen && screen.ErrorText == "Pick a culture." && !Village.IsFounded, "a good name still needs a culture");
            screen.Picker.Pick("greece");
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!screen.IsOpen && Village.Name == FirstName, $"the village is founded as '{Village.Name}' (spaces tidied)");
            Check(HudName == FirstName && SignName == FirstName, "the name shows in the HUD and on the welcome sign");
            Check(Village.CultureId == "greece", "Greece is its culture");

            ISaveService save = ServiceLocator.Get<ISaveService>();
            save.Save();
            Check(File.ReadAllText(((SaveService)save).FilePath).Contains("\"villageName\":\"Sunny Bay\""), "the name is in the save");
            PlaytestTools.CapturePortraitWithHud("m13_2_hud.png");
            PlaytestTools.FocusCamera(PlaytestTools.Find<VillageSignView>().transform.position + Vector3.up, 12f);
            PlaytestTools.CapturePortrait("m13_3_sign.png");
            AutoTestRunner.RestartPlayMode();
        }

        private static void GreetAndRename()
        {
            StartScreenView screen = Screen;
            Check(screen.IsTitlePage && screen.VillageText == FirstName, $"the next start greets with '{screen.VillageText}' and Play");
            PlaytestTools.CapturePortraitWithHud("m13_4_welcome.png");
            PlaytestTools.Click(screen, "PlayButton");
            Check(!screen.IsOpen, "Play goes to the beach");

            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SettingsButton");
            SettingsPanelView settings = PlaytestTools.Find<SettingsPanelView>();
            PlaytestTools.Click(settings, "RenameButton");
            Check(screen.IsNamePage && screen.CanClose && screen.HeadingText == "Rename your village" && screen.InputText == FirstName && !screen.ShowsCultures,
                "Rename village in Settings opens the name page with the current name and a close button, without cultures");
            PlaytestTools.CapturePortraitWithHud("m13_5_rename.png");
            screen.InputText = NewName;
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!screen.IsOpen && settings.IsOpen && Village.Name == NewName, "Save renames it and goes back to Settings");
            Check(HudName == NewName && SignName == NewName && Village.CultureId == "greece", "HUD and sign follow; the culture is unchanged");
            settings.Close();
            Visits.StartVisit(Visits.PlayerId);
        }

        private static void CheckVisit()
        {
            string title = PlaytestTools.Find<IslandHudView>().TitleText;
            Check(title == NewName + " (your saved snapshot)", $"my island's snapshot carries the name: '{title}'");
            PlaytestTools.CapturePortraitWithHud("m13_6_snapshot.png");
            Visits.ReturnHome();
        }

        private static void CheckBackHome()
        {
            StartScreenView screen = Screen;
            Check(screen != null && !screen.IsOpen, "coming home from a visit does not greet again");
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
