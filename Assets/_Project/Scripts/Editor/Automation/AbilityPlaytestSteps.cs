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
    /// Milestone 19 playtest (country abilities): the start screen cards name each country's ability and place.
    /// Founded as Italy, the build bar offers only the Gelateria (no Taverna or Tapas bar); it opens at the Cabin,
    /// sells, and every sale earns 10% more (Fatto a mano). Marco's card names both abilities. Coral's Greek island
    /// shows its Taverna and Kai's Spanish island its Tapas bar.
    /// </summary>
    internal static class AbilityPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const string Coral = "demo-coral";
        private const string Kai = "demo-kai";

        private static int _gelatoSales;

        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static GameDatabase Database => ServiceLocator.Get<GameDatabase>();
        private static VisitService Visits => ServiceLocator.Get<VisitService>();
        private static StandController Gelateria => Stands.FindByProduct("gelateria");

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Buildings.GetLot(0).state == LotState.Built;
                case 2: return BeachReady() && Buildings.MainTier == 2 && Buildings.GetLot(0).state == LotState.Built;
                case 3: return _gelatoSales > 0 && StandViewOf("gelateria") != null;
                case 4: return IslandReady("Coral Cove");
                case 5: return BeachReady();
                case 6: return IslandReady("Palm Harbour");
                case 7: return BeachReady();
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: FoundAsItaly(); break;
                case 1: CheckBuildBar(); break;
                case 2: OpenGelateria(); break;
                case 3: CheckGelateria(); break;
                case 4: CheckFriend("Coral", "Taverna", "m19_5_coral_taverna.png"); Visits.ReturnHome(); break;
                case 5: Visits.StartVisit(Kai); break;
                case 6: CheckFriend("Kai", "TapasBar", "m19_6_kai_tapas.png"); Visits.ReturnHome(); break;
                default:
                    EventBus.Unsubscribe<SaleCompleted>(OnSale);
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static void FoundAsItaly()
        {
            Time.timeScale = TestTimeScale;
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            string[] expected = { "Filoxenia", "Taverna", "Fiesta", "Tapas bar", "Fatto a mano", "Gelateria" };
            for (int i = 0; i < screen.Picker.CardCount; i++)
            {
                string text = screen.Picker.Card(i).AbilityText;
                Check(text.Contains(expected[i * 2]) && text.Contains(expected[i * 2 + 1]), $"{screen.Picker.Card(i).Label}'s card names {expected[i * 2]} and the {expected[i * 2 + 1]}");
            }

            screen.Picker.Card(2).Button.onClick.Invoke();
            PlaytestTools.CapturePortraitWithHud("m19_1_cards.png");
            screen.InputText = "Porto Gelato";
            PlaytestTools.Click(screen, "ConfirmButton");
            VillageService village = ServiceLocator.Get<VillageService>();
            Check(!screen.IsOpen && village.CultureId == "italy", $"founded as an Italian village ({village.CultureId})");

            _gelatoSales = 0;
            EventBus.Subscribe<SaleCompleted>(OnSale);
            ServiceLocator.Get<IEconomyService>().Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void CheckBuildBar()
        {
            Check(Shows("gelateria") && !Shows("taverna") && !Shows("tapas_bar"), "the build bar offers the Gelateria, not the Taverna or the Tapas bar");
            Check(Shows("donuts") && Shows("houses"), "the shared products are all still there");
            Check(!Stands.CanUnlock(Database.FindProduct("gelateria")), "the Gelateria waits for the Cabin");
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void OpenGelateria()
        {
            Check(!Stands.TryUnlockProduct(Database.FindProduct("taverna")) && !Stands.TryUnlockProduct(Database.FindProduct("tapas_bar")),
                "other countries' places can't be opened, even with coins");
            PlaytestTools.ClickProduct(System.Array.IndexOf(Database.products, Database.FindProduct("gelateria")));
            Check(Gelateria != null, $"the Gelateria opened from the build bar ({Stands.Count} stands)");
        }

        private static void CheckGelateria()
        {
            var sales = ServiceLocator.Get<SaleMultipliers>();
            CultureAbilities abilities = ServiceLocator.Get<CultureAbilities>();
            Check(abilities.Current.name == "Fatto a mano" && sales.Apply(1000, Gelateria.Product) == 1100,
                $"Fatto a mano: a 1000 coin sale pays {sales.Apply(1000, Gelateria.Product)}");
            Check(_gelatoSales > 0, $"the Gelateria sells ({_gelatoSales} sales)");
            Vector3 gelateria = StandViewOf("gelateria").transform.position;
            PlaytestTools.FocusCamera(gelateria, 16f);
            PlaytestTools.CapturePortraitWithHud("m19_2_build_bar.png");
            PlaytestTools.FocusCamera(gelateria, 9f);
            PlaytestTools.CapturePortrait("m19_3_gelateria.png");

            var card = PlaytestTools.Find<CharacterCardView>();
            EventBus.Publish(new CharacterCardRequested());
            Check(card.IsOpen && card.AbilityText.Contains("Fatto a mano") && card.AbilityText.Contains("Gelateria"),
                $"Marco's card names his country's ability and place");
            PlaytestTools.CapturePortraitWithHud("m19_4_card.png");
            card.Close();
            Visits.StartVisit(Coral);
        }

        private static void CheckFriend(string owner, string place, string shot)
        {
            GameObject own = GameObject.Find(place + "(Clone)");
            int others = 0;
            foreach (string other in new[] { "Taverna", "TapasBar", "Gelateria" })
            {
                if (other != place && GameObject.Find(other + "(Clone)") != null) others++;
            }

            Check(own != null && others == 0, $"{owner}'s island shows their {place} and no other country's place");
            if (own == null) return;
            PlaytestTools.FocusCamera(own.transform.position, 14f);
            PlaytestTools.CapturePortrait(shot);
        }

        private static void OnSale(SaleCompleted evt)
        {
            if (Gelateria != null && evt.StandIndex == Gelateria.Index) _gelatoSales++;
        }

        private static bool Shows(string productId)
        {
            foreach (ProductButtonView button in PlaytestTools.Find<BuildBarView>().GetComponentsInChildren<ProductButtonView>(true))
            {
                if (button.Product != null && button.Product.id == productId) return button.gameObject.activeSelf;
            }

            return false;
        }

        private static StandView StandViewOf(string productId)
        {
            foreach (StandView view in Object.FindObjectsByType<StandView>())
            {
                if (view.Stand != null && view.Stand.Product.id == productId) return view;
            }

            return null;
        }

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
