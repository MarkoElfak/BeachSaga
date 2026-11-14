using System.Collections.Generic;
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
    /// Milestone 21 playtest (economy balance), in a Greek village: at the Cabin the towels, café, Taverna and first
    /// aid open, and every place makes room for more visitors than the Cabin alone holds. Visitors no longer all
    /// crowd their favourite stand, so the new places sell too; Filoxenia visitors plan more purchases than anyone
    /// could before. What the beach earns over a few minutes of play is close to what the economy report says, and
    /// the report is written with every tier.
    /// </summary>
    internal static class EconomyPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float WarmUpSeconds = 60f;
        private const float MeasureSeconds = 280f; // about 35 s of real time at 8x
        private static readonly string[] Opened = { "towels", "cafe", "taverna", "first_aid" };
        private static readonly Dictionary<int, long> Sold = new Dictionary<int, long>();
        private static float _since;
        private static int _peakVisitors;
        private static int _mostPurchases;
        private static int _sales;
        private static int _arrivals;

        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static VisitorService Visitors => ServiceLocator.Get<VisitorService>();
        private static GameDatabase Database => ServiceLocator.Get<GameDatabase>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return BeachReady() && Built(1);
                case 2: return BeachReady() && Built(2);
                case 3: return Time.time - _since >= WarmUpSeconds;
                case 4: Sample(); return Time.time - _since >= MeasureSeconds;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: Begin(); break;
                case 1: Check(Buildings.TryUpgrade(0), "cabin started"); break;
                case 2: OpenPlaces(); break;
                case 3: StartMeasuring(); break;
                case 4: CheckPlay(); break;
                case 5: CheckReport(); break;
                default: Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            ServiceLocator.Get<IEconomyService>().Earn(100000);
            Check(ServiceLocator.Get<CultureService>().Current.id == "greece", "a Greek village (Filoxenia)");
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void OpenPlaces()
        {
            foreach (string id in Opened)
            {
                ProductDefinition product = Database.FindProduct(id);
                Check(Stands.TryUnlockProduct(product), $"{product.displayName} opened for {product.unlockCost} coins");
            }

            TierDefinition cabin = Database.GetTier(2);
            float stay = ServiceLocator.Get<CultureAbilities>().StayMultiplier;
            int room = EconomyMath.BeachRoom(cabin.maxVisitors, Stands.Count, Database.visitors.visitorsPerPlace, stay, Database.visitors.poolMaxSize);
            Check(room > cabin.maxVisitors, $"{Stands.Count} places make room for {room} visitors (the Cabin alone: {cabin.maxVisitors})");
            _since = Time.time;
        }

        private static void StartMeasuring()
        {
            Sold.Clear();
            _peakVisitors = 0;
            _mostPurchases = 0;
            _sales = 0;
            _arrivals = 0;
            EventBus.Subscribe<SaleCompleted>(OnSale);
            EventBus.Subscribe<VisitorSpawned>(OnArrival);
            _since = Time.time;
        }

        private static void OnSale(SaleCompleted evt)
        {
            Sold.TryGetValue(evt.StandIndex, out long coins);
            Sold[evt.StandIndex] = coins + evt.Amount;
            _sales++;
        }

        private static void OnArrival(VisitorSpawned evt) => _arrivals++;

        // The report leaves events out, so a running one ends at once (a tourist bus would crowd the beach).
        private static void Sample()
        {
            BeachEventService events = ServiceLocator.Get<BeachEventService>();
            if (events.IsActive) events.End();
            _peakVisitors = Mathf.Max(_peakVisitors, Visitors.ActiveCount);
            foreach (VisitorAgent agent in Visitors.Active) _mostPurchases = Mathf.Max(_mostPurchases, agent.MaxPurchases);
        }

        private static void CheckPlay()
        {
            Unsubscribe();
            int cabin = Database.GetTier(2).maxVisitors, most = Database.visitors.purchasesRange.y;
            Check(_peakVisitors > cabin, $"up to {_peakVisitors} visitors on the beach at once (the Cabin alone holds {cabin})");
            Check(_mostPurchases > most, $"Filoxenia: a visitor planned {_mostPurchases} purchases (at most {most} elsewhere)");
            Check(SoldAt("cafe") > 0 && SoldAt("taverna") > 0 && SoldAt("towels") > 0,
                $"the new places sell too: towels {SoldAt("towels")}, café {SoldAt("cafe")}, Taverna {SoldAt("taverna")} coins");

            double measured = 0.0, hours = (Time.time - _since) / 3600.0;
            foreach (long coins in Sold.Values) measured += coins;
            measured /= hours;
            var places = new List<ProductDefinition>();
            for (int i = 0; i < Stands.Count; i++) places.Add(Stands.Get(i).Product);
            var model = new EconomyModel(Database, ServiceLocator.Get<CultureService>().Current);
            EconomyResult report = model.Run(2, places, new List<ProductDefinition>());
            double expected = report.Sales, ratio = measured / expected;
            AutoTestRunner.Write($"  measured {_arrivals / hours:0} visitors/h and {_sales / hours:0} sales/h; the report: {report.Visitors:0} visitors/h");
            Check(ratio > 0.6 && ratio < 1.5, $"the beach earned {EconomyReport.Coins(measured)} coins an hour; the report says {EconomyReport.Coins(expected)} ({ratio:P0})");

            Bounds beach = new Bounds(Vector3.zero, Vector3.one);
            foreach (StandView view in Object.FindObjectsByType<StandView>()) beach.Encapsulate(view.transform.position);
            PlaytestTools.FocusCamera(beach.center, 18f + beach.size.x);
            PlaytestTools.CapturePortraitWithHud("m21_1_busy_cabin.png");
        }

        private static void CheckReport()
        {
            string text = EconomyReport.Write(Database);
            bool everyTier = true;
            for (int tier = 1; tier <= Database.MaxTier; tier++) everyTier &= text.Contains($"| {tier} {Database.GetTier(tier).displayName} |");
            Check(everyTier && text.Contains("Pays back in") && text.Contains("Countries"), $"the economy report lists every tier, the new places and the countries ({EconomyReport.RelativePath})");
        }

        private static long SoldAt(string productId)
        {
            StandController stand = Stands.FindByProduct(productId);
            return stand != null && Sold.TryGetValue(stand.Index, out long coins) ? coins : 0L;
        }

        private static void Finish()
        {
            Unsubscribe();
            AutoTestRunner.Finish();
        }

        private static void Unsubscribe()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            EventBus.Unsubscribe<VisitorSpawned>(OnArrival);
        }

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
