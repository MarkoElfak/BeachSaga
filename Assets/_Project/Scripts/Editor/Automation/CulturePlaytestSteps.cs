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
    /// Milestone 14 playtest (cultures): a new game shows one card per culture and needs one to found the village;
    /// the main building flies the Greek flag at every tier in Greek colours, and the HUD shows the flag. A village
    /// from before cultures is asked once for its culture (Italy) and is redressed; the greeting shows the flag and
    /// the rename page has no cultures. Friends' islands fly their own flags (Kai: Spain, Marina: Italy).
    /// </summary>
    internal static class CulturePlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const int TopTier = 5;
        private const string Kai = "demo-kai";
        private const string Marina = "demo-marina";

        private static VillageService Village => ServiceLocator.Get<VillageService>();
        private static CultureService Cultures => ServiceLocator.Get<CultureService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static VisitService Visits => ServiceLocator.Get<VisitService>();
        private static StartScreenView Screen => PlaytestTools.Find<StartScreenView>();
        private static FlagView MainFlag => PlaytestTools.Find<FlagView>();

        public static bool IsReady(int step)
        {
            if (step >= 1 && step <= TopTier)
            {
                return BeachReady() && Buildings.GetLot(0).state == LotState.Built && Buildings.MainTier == step && MainFlag != null;
            }

            switch (step)
            {
                case 6: return BeachReady() && Screen != null && Screen.IsOpen;
                case 7: return BeachReady() && MainFlag != null && MainFlag.Shown == FlagOf("italy");
                case 8: return BeachReady() && Screen != null && Screen.IsOpen;
                case 9: return IslandReady("Palm Harbour");
                case 10: return BeachReady();
                case 11: return IslandReady("Marina Bay");
                case 12: return BeachReady();
                default: return true;
            }
        }

        public static void Run(int step)
        {
            if (step >= 1 && step <= TopTier)
            {
                CheckTier(step);
                return;
            }

            switch (step)
            {
                case 0: FoundWithCulture(); break;
                case 6: ChooseForOldVillage(); break;
                case 7: CheckRedressed(); break;
                case 8: CheckGreeting(); break;
                case 9: CheckFriend("Kai", "spain", "m14_8_kai.png"); Visits.ReturnHome(); break;
                case 10: Visits.StartVisit(Marina); break;
                case 11: CheckFriend("Marina", "italy", "m14_9_marina.png"); Visits.ReturnHome(); break;
                default: AutoTestRunner.Finish(); break;
            }
        }

        private static void FoundWithCulture()
        {
            Time.timeScale = TestTimeScale;
            StartScreenView screen = Screen;
            CulturePickerView picker = screen.Picker;
            Check(screen.IsNamePage && screen.ShowsNameField && screen.ShowsCultures, "a new game asks for the name and the culture together");
            Check(picker.CardCount == 3 && picker.Card(0).CultureId == "greece" && picker.Card(1).CultureId == "spain" && picker.Card(2).CultureId == "italy",
                "three culture cards: Greece, Spain, Italy");
            for (int i = 0; i < picker.CardCount; i++)
            {
                CultureCardView card = picker.Card(i);
                Check(card.Label == Cultures.Find(card.CultureId).displayName && FlagOf(card.CultureId) != null, $"card '{card.Label}' has its name and flag");
            }

            PlaytestTools.CapturePortraitWithHud("m14_1_found.png");
            screen.InputText = "Kalamaki";
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!Village.IsFounded && screen.ErrorText == "Pick a character.", "founding without a culture is refused");

            picker.Card(0).Button.onClick.Invoke();
            Check(picker.PickedId == "greece" && picker.Card(0).IsPicked && !picker.Card(1).IsPicked && screen.ErrorText.Length == 0,
                "tapping Greece highlights only that card and clears the message");
            PlaytestTools.Click(screen, "IdeaButton");
            Check(System.Array.IndexOf(Cultures.Find("greece").villageNames, screen.InputText) >= 0, $"Idea suggests a Greek name: '{screen.InputText}'");
            PlaytestTools.CapturePortraitWithHud("m14_2_picked.png");

            screen.InputText = "Kalamaki";
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!screen.IsOpen && Village.Name == "Kalamaki" && Village.CultureId == "greece", "Kalamaki is founded as a Greek village");
            Check(!Village.TryChooseCulture("spain", out string error) && error == "Your character is already chosen.", "the culture cannot be changed");
            Check(PlaytestTools.Find<VillageNameView>().Flag == FlagOf("greece"), "the HUD shows the Greek flag");

            ServiceLocator.Get<IEconomyService>().Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void CheckTier(int tier)
        {
            Check(MainFlag.Shown == FlagOf("greece"), $"tier {tier} flies the Greek flag");
            Check(IsDressed(MainFlag, "greece"), $"tier {tier} has Greek walls and roofs");
            PlaytestTools.FocusCamera(MainFlag.transform.position, 9f + tier * 2f);
            PlaytestTools.CapturePortrait($"m14_3_tier{tier}.png");
            if (tier < TopTier)
            {
                Check(Buildings.TryUpgrade(0), $"tier {tier + 1} started");
                return;
            }

            // Turn the save into one from before cultures: it must ask for the culture once.
            ISaveService save = ServiceLocator.Get<ISaveService>();
            save.Save();
            Check(File.ReadAllText(((SaveService)save).FilePath).Contains("\"cultureId\":\"greece\""), "the culture is in the save");
            save.Data.meta.cultureId = string.Empty;
            save.Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void ChooseForOldVillage()
        {
            Time.timeScale = TestTimeScale;
            StartScreenView screen = Screen;
            Check(Village.IsFounded && Village.NeedsCulture && screen.IsNamePage, "a village without a culture is asked for one");
            Check(screen.HeadingText == "Choose your character" && !screen.ShowsNameField && screen.ShowsCultures && !screen.CanClose,
                "the page shows only the cultures, with no way to skip");
            Check(PlaytestTools.Find<VillageNameView>().Flag == null, "the HUD has no flag yet");
            PlaytestTools.CapturePortraitWithHud("m14_4_choose.png");
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(screen.IsOpen && screen.ErrorText == "Pick a character.", "Confirm without a pick is refused");
            screen.Picker.Card(2).Button.onClick.Invoke();
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!screen.IsOpen && Village.CultureId == "italy" && Village.Name == "Kalamaki", "Italy is chosen and the name is kept");
        }

        private static void CheckRedressed()
        {
            Check(IsDressed(MainFlag, "italy") && Buildings.MainTier == TopTier, "the Resort is redressed in Italian colours with the Italian flag");
            Check(PlaytestTools.Find<VillageNameView>().Flag == FlagOf("italy"), "the HUD shows the Italian flag");
            PlaytestTools.CapturePortraitWithHud("m14_5_italy.png");
            ServiceLocator.Get<ISaveService>().Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckGreeting()
        {
            StartScreenView screen = Screen;
            Check(screen.IsTitlePage && screen.TitleFlag == FlagOf("italy") && screen.CultureText == "Italy" && screen.VillageText == "Kalamaki",
                $"the greeting shows the Italian flag, '{screen.CultureText}' and the name");
            PlaytestTools.CapturePortraitWithHud("m14_6_welcome.png");
            PlaytestTools.Click(screen, "PlayButton");

            screen.OpenRename();
            Check(screen.IsNamePage && screen.ShowsNameField && !screen.ShowsCultures, "renaming does not offer the cultures");
            PlaytestTools.CapturePortraitWithHud("m14_7_rename.png");
            PlaytestTools.Click(screen, "CloseButton");
            Visits.StartVisit(Kai);
        }

        private static void CheckFriend(string owner, string cultureId, string shot)
        {
            IslandPropSpawner props = PlaytestTools.Find<IslandPropSpawner>();
            FlagView flag = MainFlag;
            Check(props.Culture != null && props.Culture.id == cultureId, $"{owner}'s island is {cultureId}");
            Check(flag != null && flag.Shown == FlagOf(cultureId) && IsDressed(flag, cultureId), $"{owner}'s main building flies the {cultureId} flag in its colours");
            Check(PlaytestTools.Find<IslandHudView>().Flag == FlagOf(cultureId), $"the island banner shows the {cultureId} flag");
            PlaytestTools.CapturePortraitWithHud(shot);
            if (flag == null) return;
            PlaytestTools.FocusCamera(flag.transform.position, 16f);
            PlaytestTools.CapturePortrait(shot.Replace(".png", "_flag.png"));
        }

        // Some renderer of the flag's building wears a material repainted for this culture.
        private static bool IsDressed(FlagView flag, string cultureId)
        {
            BuildingView building = flag.GetComponentInParent<BuildingView>();
            if (building == null) return false;
            string tag = $"({cultureId})";
            foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name.EndsWith(tag)) return true;
                }
            }

            return false;
        }

        private static Texture FlagOf(string cultureId) => Cultures.Flag(Cultures.Find(cultureId));

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<BuildBarView>() != null;

        private static bool IslandReady(string village)
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Island) return false;
            IslandHudView hud = PlaytestTools.Find<IslandHudView>();
            IslandPropSpawner props = PlaytestTools.Find<IslandPropSpawner>();
            return hud != null && hud.TitleText.StartsWith(village) && props != null && props.SpawnedCount > 0;
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
