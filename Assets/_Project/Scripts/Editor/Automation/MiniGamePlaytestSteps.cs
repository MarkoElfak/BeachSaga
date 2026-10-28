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
    /// Mini-games playtest: builds up to the Resort with every stand and checks the surf school stands at the
    /// water. Then, for each stand: select it, press Play, read the intro and let the game play itself (autopilot)
    /// through level 1, checking the reward (sales bonus, stand decoration). Donut rush then goes through all five
    /// levels plus a replay, a towel run is lost on purpose, a real sale includes the bonus and a restart keeps it.
    /// </summary>
    internal static partial class MiniGamePlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float GameSpeed = 4f;
        private const int FirstGameStep = 6;
        private const int StepsPerGame = 3;
        private static readonly string[] Products = { "donuts", "towels", "drinks", "surf_lessons", "houses" };

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static MiniGameService Games => ServiceLocator.Get<MiniGameService>();
        private static MiniGamePanelView Panel => PlaytestTools.Find<MiniGamePanelView>();
        private static int AllGamesDone => FirstGameStep + Products.Length * StepsPerGame;

        public static bool IsReady(int step)
        {
            if (step >= FirstGameStep && step < AllGamesDone) return IsGameStepReady((step - FirstGameStep) % StepsPerGame, Products[(step - FirstGameStep) / StepsPerGame]);
            int s = step - AllGamesDone;
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2;
                case 3: return Buildings.MainTier == 3;
                case 4: return Buildings.MainTier == 4;
                case 5: return Buildings.MainTier == 5 && Stands.FindByProduct("drinks") != null;
            }

            if (s == 0) return Panel.ShowsIntro;
            if (s == 5) return TapWantedIcing();
            if (s >= 1 && s <= 6) return Panel.ShowsResult;
            if (s == 7) return _saleChecked;
            if (s == 8) return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
            return true;
        }

        public static void Run(int step)
        {
            if (step >= FirstGameStep && step < AllGamesDone)
            {
                int game = (step - FirstGameStep) / StepsPerGame;
                RunGameStep((step - FirstGameStep) % StepsPerGame, game);
                return;
            }

            int s = step - AllGamesDone;
            if (step == 0) Setup();
            else if (step <= 4) BuildUp();
            else if (step == 5) CheckStandsAndShore();
            else if (s == 0) Click("PlayButton");
            else if (s >= 1 && s <= 4) CheckDonutLevel(s + 1);
            else if (s == 5) CheckReplay();
            else if (s == 6) CheckLostRun();
            else if (s == 7) CheckSaleAndRestart();
            else if (s == 8) CheckAfterRestart();
            else Done();
        }

        private static void Setup()
        {
            Time.timeScale = TestTimeScale;
            ResetChecks();
            MiniGamePanelView.Autopilot = true;
            MiniGamePanelView.SpeedMultiplier = GameSpeed;
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            Check(Games.Count == ServiceLocator.Get<GameDatabase>().products.Length, $"{Games.Count} mini-games, one per stand");
            ServiceLocator.Get<IEconomyService>().Earn(2000000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void BuildUp()
        {
            UnlockProducts();
            Check(Buildings.TryUpgrade(0), $"tier {Buildings.MainTier + 1} started");
        }

        // The shared products plus the village's own country place.
        private static int OpenProductCount()
        {
            int count = 0;
            foreach (ProductDefinition product in ServiceLocator.Get<GameDatabase>().products) if (Stands.IsAvailable(product)) count++;
            return count;
        }

        private static void UnlockProducts()
        {
            foreach (ProductDefinition product in ServiceLocator.Get<GameDatabase>().products)
            {
                if (Stands.CanUnlock(product)) Stands.TryUnlockProduct(product);
            }
        }

        private static void Done()
        {
            MiniGamePanelView.Autopilot = false;
            MiniGamePanelView.SpeedMultiplier = 1f;
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            AutoTestRunner.Finish();
        }

        // Screenshots catch a moment with something going on: a visitor waiting, a drink pouring, a floor sliding.
        private static bool ShowsAction(MiniGame game)
        {
            switch (game)
            {
                case TowelRunGame towels:
                    for (int i = 0; i < towels.Beds; i++)
                    {
                        if (towels.IsWaiting(i)) return true;
                    }

                    return false;
                case PerfectPourGame pour: return pour.Pouring && pour.Fill > 0.3f;
                case SkyBuilderGame sky: return !sky.Resting;
                default: return true;
            }
        }

        private static void Click(string button) => PlaytestTools.Click(Panel, button);

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
