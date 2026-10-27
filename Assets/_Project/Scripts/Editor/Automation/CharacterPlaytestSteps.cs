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
    /// Milestone 17 playtest (your character): the start screen shows Nikos, Lucía and Marco with portraits and
    /// abilities; picking Spain brings Lucía, who walks to the first stand once it opens and serves it 25% faster.
    /// Tapping her opens her card; Work here on the towel stand sends her over and the bonus moves with her. She is
    /// still there after a restart, and a save from before characters puts her at the first stand.
    /// </summary>
    internal static class CharacterPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float Boosted = 1.25f;

        private static CharacterService Characters => ServiceLocator.Get<CharacterService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static CharacterView View => PlaytestTools.Find<CharacterView>();
        private static CharacterCardView Card => PlaytestTools.Find<CharacterCardView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Stands.Count > 0 && Characters.IsWorking && Buildings.GetLot(0).state == LotState.Built;
                case 2: return BeachReady() && Buildings.MainTier == 2 && Buildings.GetLot(0).state == LotState.Built;
                case 3: return Characters.IsWorking && Characters.StandIndex == 1;
                case 4:
                case 5: return BeachReady() && View != null && Characters.IsWorking;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: PickLucia(); break;
                case 1: CheckAtWork(); break;
                case 2: SendToTowels(); break;
                case 3: CheckMoved(); break;
                case 4: CheckAfterRestart(); break;
                case 5: CheckOldSave(); break;
                default: AutoTestRunner.Finish(); break;
            }
        }

        private static void PickLucia()
        {
            Time.timeScale = TestTimeScale;
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            CulturePickerView picker = screen.Picker;
            Check(screen.IsNamePage && picker.CardCount == 3, "a new game shows three character cards");
            string[] names = { "Nikos", "Lucía", "Marco" };
            for (int i = 0; i < picker.CardCount && i < names.Length; i++)
            {
                CultureCardView card = picker.Card(i);
                Check(card.CharacterName == names[i] && card.PortraitImage != null && card.AbilityText.Contains("25%"),
                    $"card {i + 1} is {card.CharacterName} of {card.Label}, with a portrait and the ability");
            }

            Check(!Characters.HasJoined && View == null, "nobody walks the beach before a character is picked");
            PlaytestTools.CapturePortraitWithHud("m17_1_cards.png");
            screen.InputText = "Cala Test";
            picker.Card(1).Button.onClick.Invoke();
            PlaytestTools.Click(screen, "ConfirmButton");
            Check(!screen.IsOpen && Characters.HasJoined && Characters.Character.displayName == "Lucía", "picking Spain brings Lucía");
            Check(View != null && Characters.StandIndex == -1 && !Characters.IsWorking, "Lucía waits on the beach until a stand opens");
            Check(Card.ShowsButton, "her portrait button is on the HUD");

            ServiceLocator.Get<IEconomyService>().Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void CheckAtWork()
        {
            Check(Characters.StandIndex == 0 && Stands.Get(0).ServeSpeed == Boosted, "she walked to the donut stand, which now serves 25% faster");
            Check(View.GetComponentInChildren<NameTagView>().Text == "Lucía", "her name tag reads Lucía");
            Vector3 at = View.transform.position;
            PlaytestTools.FocusCamera(at, 8f);
            PlaytestTools.CapturePortrait("m17_2_at_work.png");

            PlaytestTools.TapWorld(at + Vector3.up * 0.9f);
            Check(Card.IsOpen && Card.NameText == "Lucía" && Card.WorkText == "Works at Donuts",
                $"tapping her opens her card ({PlaytestTools.HitName(at + Vector3.up * 0.9f)}): '{Card.WorkText}'");
            PlaytestTools.CapturePortraitWithHud("m17_3_card.png");
            Card.Close();
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void SendToTowels()
        {
            Check(Stands.TryUnlockProduct(ServiceLocator.Get<GameDatabase>().FindProduct("towels")), "towel stand opened");
            Check(Characters.StandIndex == 0, "she stays at her stand when another opens");
            Vector3 towels = StandPosition(1);
            PlaytestTools.FocusCamera(towels, 12f);
            PlaytestTools.TapWorld(towels + Vector3.up);
            SelectionArrowsView bar = PlaytestTools.Find<SelectionArrowsView>();
            Check(FindButton(bar, "WorkButton").gameObject.activeSelf, $"the towel stand offers Work here ({PlaytestTools.HitName(towels + Vector3.up)})");
            PlaytestTools.CapturePortraitWithHud("m17_4_work_here.png");
            PlaytestTools.Click(bar, "WorkButton");
            Check(Characters.StandIndex == 1 && Characters.IsWalking && Stands.Get(0).ServeSpeed == 1f, "she walks over and the donut stand is back to normal");

        }

        private static void CheckMoved()
        {
            Check(Stands.Get(1).ServeSpeed == Boosted && Stands.Get(0).ServeSpeed == 1f, "the towel stand now serves 25% faster");
            PlaytestTools.FocusCamera(View.transform.position, 8f);
            PlaytestTools.CapturePortrait("m17_5_towels.png");
            PlaytestTools.Click(Card.transform.parent, "CharacterButton");
            Check(Card.IsOpen && Card.WorkText == "Works at Towels", $"the HUD button opens her card: '{Card.WorkText}'");
            Card.Close();
            ServiceLocator.Get<ISaveService>().Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckAfterRestart()
        {
            CloseGreeting();
            Check(Characters.StandIndex == 1 && Stands.Get(1).ServeSpeed == Boosted && !Characters.IsWalking,
                "after a restart she is already at work at the towel stand");

            // A save from before characters: no stand recorded.
            ISaveService save = ServiceLocator.Get<ISaveService>();
            save.Data.meta.characterStand = -1;
            save.Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckOldSave()
        {
            CloseGreeting();
            Check(Characters.Character.displayName == "Lucía" && Characters.StandIndex == 0 && Stands.Get(0).ServeSpeed == Boosted,
                "an older save gets the character of its culture at the first stand");
        }

        private static void CloseGreeting()
        {
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            if (screen != null && screen.IsOpen) screen.Close();
        }

        private static Vector3 StandPosition(int index)
        {
            ServiceLocator.Get<WorldNavigation>().TryGetStandPosition(index, out Vector3 position);
            return position;
        }

        private static UnityEngine.UI.Button FindButton(Component owner, string name)
        {
            foreach (UnityEngine.UI.Button button in owner.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (button.name == name) return button;
            }

            return null;
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
