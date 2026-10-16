using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The M9 playtest's checks: each event's effect, the banner, and day versus night.</summary>
    internal static partial class EventPlaytestSteps
    {
        private static int _baseTotal, _baseDrinks, _heatTotal, _heatDrinks;
        private static float _baselineShare;
        private static string _watchProduct;
        private static bool _saleChecked, _saleOk, _watching;
        private static string _saleText;
        private static Color _noonSun, _noonFog;
        private static float _noonIntensity;

        private static DayNightView Sky => PlaytestTools.Find<DayNightView>();

        // Static state outlives a run (no domain reload between playtests), and the bus was cleared on boot.
        private static void ResetChecks()
        {
            _watching = false;
            _saleChecked = false;
        }

        private static void CheckTouristBus()
        {
            BeachEventDefinition bus = Definition("TouristBus");
            float baseline = Visitors.SpawnInterval;
            int before = Visitors.ActiveCount;
            Check(Events.Begin(bus), "tourist bus started");
            Check(Visitors.ActiveCount - before == bus.burstVisitors, $"a busload arrives at once ({before} -> {Visitors.ActiveCount} visitors)");
            Check(Mathf.Approximately(Visitors.SpawnInterval, baseline / bus.spawnRateMultiplier),
                $"visitors arrive {bus.spawnRateMultiplier:0}x as often ({baseline:0.00} s -> {Visitors.SpawnInterval:0.00} s)");
            Check(Banner.IsShowing && Banner.TitleText == bus.title && Banner.TimerText == "1:30", $"banner shows '{Banner.TitleText}' with {Banner.TimerText} left");
            var analytics = (MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>();
            Check(analytics.HasLogged(AnalyticsEvents.BeachEventStarted), "analytics: beach_event_started");
            PlaytestTools.CapturePortraitWithHud("m9_1_tourist_bus.png");
        }

        private static void CheckBusEnded()
        {
            float expected = ServiceLocator.Get<GameDatabase>().economy.baseSpawnIntervalSeconds / (1f + ServiceLocator.Get<PopularityService>().Value);
            Check(!Banner.IsShowing && Mathf.Approximately(Visitors.SpawnInterval, expected), $"the bus leaves after its time: banner gone, arrivals back to {expected:0.00} s");
        }

        // A quiet stretch with no event first: the share of orders drinks gets normally.
        private static void StartBaseline()
        {
            _baseTotal = Total();
            _baseDrinks = Drinks();
        }

        private static void StartHeatwave()
        {
            _baselineShare = Share(Drinks() - _baseDrinks, Total() - _baseTotal);
            Check(!Events.IsActive, $"no event: drinks get {_baselineShare:P0} of {Total() - _baseTotal} orders");
            Check(Events.Begin(Definition("Heatwave")), "heatwave started");
            _heatTotal = Total();
            _heatDrinks = Drinks();
            WatchSale("drinks");
        }

        private static void CheckHeatwave()
        {
            Check(_saleOk, _saleText);
            float share = Share(Drinks() - _heatDrinks, Total() - _heatTotal);
            Check(share >= _baselineShare + 0.15f, $"drinks get more of the orders in a heatwave ({_baselineShare:P0} -> {share:P0} of {Total() - _heatTotal})");
            Events.End();
            Check(!Banner.IsShowing && Events.PriceMultiplier(Stands.FindByProduct("drinks").Product) == 1f, "ending the heatwave puts drink prices back");
        }

        private static void StartSurfContest()
        {
            Check(Events.Begin(Definition("SurfContest")), "surf contest started");
            Check(Banner.IsShowing && Banner.TitleText == "Surf contest", $"banner shows '{Banner.TitleText}' with {Banner.TimerText} left");
            PlaytestTools.CapturePortraitWithHud("m9_2_surf_contest.png");
            WatchSale("surf_lessons");
        }

        private static void CheckSurfContest()
        {
            Check(_saleOk, _saleText);
            Events.End();
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            _watching = false;
        }

        private static void RecordMidday()
        {
            _noonSun = Sky.SunColor;
            _noonFog = Sky.FogColor;
            _noonIntensity = Sky.SunIntensity;
            DayNightConfig config = ServiceLocator.Get<GameDatabase>().dayNight;
            Check(Distance(_noonSun, config.sunColor.Evaluate(0.25f)) < 0.05f, $"midday: warm sun at {_noonIntensity:0.00}");
        }

        private static void CheckNight()
        {
            Color sun = Sky.SunColor;
            Check(Sky.SunIntensity < _noonIntensity && sun.b > sun.r, $"night: dimmer, bluer sun ({_noonIntensity:0.00} -> {Sky.SunIntensity:0.00})");
            Check(Sky.FogColor.grayscale < _noonFog.grayscale, "night: darker sky and haze");
            Check(Clock.IsNight, "the clock reports night");
            PlaytestTools.CapturePortraitWithHud("m9_3_night.png");
        }

        private static bool ShowsPhase(float phase) => Sky != null && Mathf.Abs(Sky.AppliedPhase - phase) < 0.01f;

        // The next sale of this product is checked against its price x spend x level x boost x the event.
        private static void WatchSale(string productId)
        {
            _watchProduct = productId;
            _saleChecked = false;
            if (_watching) return;
            EventBus.Subscribe<SaleCompleted>(OnSale);
            _watching = true;
        }

        private static void OnSale(SaleCompleted evt)
        {
            StandController stand = Stands.Get(evt.StandIndex);
            if (_saleChecked || evt.VisitorId < 0 || stand.Product.id != _watchProduct) return;

            float spend = Visitors.Get(evt.VisitorId).Type.spendMultiplier;
            float bonus = ServiceLocator.Get<GameDatabase>().economy.standLevelIncomeBonus;
            float eventMultiplier = Events.PriceMultiplier(stand.Product);
            long plain = EconomyMath.ApplyMultiplier(EconomyMath.SaleValue(stand.Product.price, spend, stand.Model.level, bonus),
                ServiceLocator.Get<IncomeBoost>().Multiplier);
            plain = EconomyMath.ApplyMultiplier(plain, ServiceLocator.Get<SeasonService>().IncomeMultiplier);
            long expected = EconomyMath.ApplyMultiplier(plain, eventMultiplier);
            _saleOk = evt.Amount == expected && eventMultiplier > 1f;
            _saleText = $"{stand.Product.displayName} sell for {eventMultiplier:0.#}x during the event ({plain} -> {evt.Amount} coins)";
            _saleChecked = true;
        }

        private static int Total()
        {
            int total = 0;
            for (int i = 0; i < Stands.Count; i++) total += Stands.Get(i).TotalOrders;
            return total;
        }

        private static int Drinks() => Stands.FindByProduct("drinks").TotalOrders;
        private static float Share(int part, int total) => total > 0 ? part / (float)total : 0f;
        private static float Distance(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
