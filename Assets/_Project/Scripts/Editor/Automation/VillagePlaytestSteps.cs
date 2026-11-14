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
    /// Milestone 20 playtest (village buildings): the build bar has Stands, Food, Services and Decor tabs. At the Cabin
    /// the beach café and the first-aid station open on spots of their own; a Care worker at first aid lets visitors
    /// wait longer within its ring. At the Bungalow the restaurant takes two workers who serve as a team and the
    /// (Greek) chapel adds coins and popularity in reach and holds a wedding. At the Resort first aid grows into a
    /// hospital, and everything is still there after a restart.
    /// </summary>
    internal static partial class VillagePlaytestSteps
    {
        private const float TestTimeScale = 8f;

        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static StaffService Staff => ServiceLocator.Get<StaffService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static ServiceBuildings Services => ServiceLocator.Get<ServiceBuildings>();
        private static PlacementService Placement => ServiceLocator.Get<PlacementService>();
        private static GameDatabase Database => ServiceLocator.Get<GameDatabase>();
        private static BuildBarView Bar => PlaytestTools.Find<BuildBarView>();
        private static StandController FirstAid => Stands.FindByProduct("first_aid");
        private static StandController Chapel => Stands.FindByProduct("chapel");
        private static StandController Restaurant => Stands.FindByProduct("restaurant");

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Built(1);
                case 2: return BeachReady() && Built(2) && Staff.Seekers.Count >= 1;
                case 3: return Built(3) && AtWork(FirstAid, 1) && Staff.Seekers.Count >= 3;
                case 4: return AtWork(Restaurant, 2) && AtWork(Chapel, 1);
                case 5: return Built(4);
                case 6: return Built(5);
                case 7: return BeachReady() && ViewOf("first_aid") != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: Begin(); break;
                case 1: CheckTabs(); break;
                case 2: OpenCafeAndFirstAid(); break;
                case 3: CheckFirstAid(); break;
                case 4: CheckRestaurantAndChapel(); break;
                case 5: Check(Buildings.TryUpgrade(0), "resort started"); break;
                case 6: CheckHospital(); break;
                case 7: CheckAfterRestart(); break;
                default: Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            EventBus.Subscribe<WeddingHeld>(OnWedding);
            ServiceLocator.Get<IEconomyService>().Earn(5000000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void CheckTabs()
        {
            Check(Bar.TabCount == 4 && Bar.Tab == 0, $"the build bar has {Bar.TabCount} tabs and opens on Stands");
            Check(Shows("donuts") && !Shows("cafe") && !Shows("chapel"), "Stands lists the stands only");
            PlaytestTools.Click(Bar, "FoodTab");
            Check(Bar.Tab == 1 && Shows("cafe") && Shows("restaurant") && Shows("taverna") && !Shows("donuts"),
                "Food lists the café, the restaurant and the country's Taverna");
            PlaytestTools.CapturePortraitWithHud("m20_1_food_tab.png");
            PlaytestTools.Click(Bar, "ServicesTab");
            Check(Shows("chapel") && Shows("first_aid") && !Shows("cafe"), "Services lists the chapel and first aid");
            PlaytestTools.Click(Bar, "DecorTab");
            Check(Bar.Tab == BuildBarView.DecorTab && !Shows("chapel") && DecorTilesShown() > 0, $"Decor lists {DecorTilesShown()} decorations");
            PlaytestTools.CapturePortraitWithHud("m20_2_decor_tab.png");
            Bar.ShowTab(0);
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void OpenCafeAndFirstAid()
        {
            Open("cafe");
            Open("first_aid");
            Check(FirstAid.Product.IsService && !FirstAid.TryEnqueueOrder(0, 1f), "first aid takes no orders");
            StaffMember carer = BestAt(StaffSkill.Care);
            Check(Staff.TryHire(carer.Id, FirstAid.Index), $"{carer.Model.name} ({carer.Model.Stars(StaffSkill.Care)} Care stars) hired for first aid");
            Check(Buildings.TryUpgrade(0), "bungalow started");
        }

        // Opens a village building from the build bar; it must stand on a free spot of its own.
        private static void Open(string productId)
        {
            ProductDefinition product = Database.FindProduct(productId);
            Check(Stands.CanUnlock(product), $"{product.displayName} can be opened");
            PlaytestTools.ClickProduct(System.Array.IndexOf(Database.products, product));
            StandController stand = Stands.FindByProduct(productId);
            Check(stand != null, $"{product.displayName} opened");
            if (stand == null) return;
            PlaceableId id = PlaceableId.Stand(stand.Index);
            Vector2 at = Placement.GetStandPosition(stand.Index);
            Check(Placement.CanPlace(id, at), $"{product.displayName} stands on a free spot ({at.x:0.0}, {at.y:0.0})");
        }

        private static void Finish()
        {
            EventBus.Unsubscribe<WeddingHeld>(OnWedding);
            AutoTestRunner.Finish();
        }

        private static bool Built(int tier) => Buildings.MainTier == tier && Buildings.GetLot(0).state == LotState.Built;

        private static bool AtWork(StandController stand, int people) => stand != null && stand.People >= people && !AnyWalking();

        private static bool AnyWalking()
        {
            foreach (StaffMember member in Staff.Staff) if (member.IsWalking) return true;
            return false;
        }

        private static StaffMember BestAt(StaffSkill skill)
        {
            StaffMember best = null;
            foreach (StaffMember seeker in Staff.Seekers)
            {
                if (best == null || seeker.Model.Stars(skill) > best.Model.Stars(skill)) best = seeker;
            }

            return best;
        }

        private static bool Shows(string productId)
        {
            foreach (ProductButtonView button in Bar.GetComponentsInChildren<ProductButtonView>(true))
            {
                if (button.Product != null && button.Product.id == productId) return button.gameObject.activeSelf;
            }

            return false;
        }

        private static int DecorTilesShown()
        {
            int shown = 0;
            foreach (DecorButtonView tile in Bar.GetComponentsInChildren<DecorButtonView>(false)) shown++;
            return shown;
        }

        private static StandView ViewOf(string productId)
        {
            foreach (StandView view in Object.FindObjectsByType<StandView>())
            {
                if (view.Stand != null && view.Stand.Product.id == productId) return view;
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
