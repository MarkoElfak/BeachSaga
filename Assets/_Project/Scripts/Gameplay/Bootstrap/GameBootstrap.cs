using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Composition root. Lives in the Boot scene, creates every plain C# service in a fixed order,
    /// ticks them each frame, saves on pause/quit and then loads the Beach scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameDatabase database = null;

        private static GameBootstrap _instance;

        private readonly List<ITickable> _tickables = new List<ITickable>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private SaveService _save;

        public static bool IsReady => _instance != null;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (database == null)
            {
                Debug.LogError("GameBootstrap needs a GameDatabase. Run Beach Tycoon > Setup Project.", this);
                enabled = false;
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = database.presentation.targetFrameRate;

            EventBus.ClearAll();
            ServiceLocator.Reset();
            CreateServices();

            SceneManager.LoadScene(SceneNames.Beach);
        }

        // Order follows the design doc: Save, Economy, Progression, Building, Visitors, Neighbours, UI.
        // Platform seams (analytics, ads, IAP) come right after Save so every later service can use them.
        private void CreateServices()
        {
            ServiceLocator.Register(database);
            var clock = new SystemTimeProvider();
            ServiceLocator.Register<ITimeProvider>(clock);

            _save = new SaveService(Application.persistentDataPath, database.economy.autosaveSeconds,
                database.economy.startingCoins, clock);
            ServiceLocator.Register<ISaveService>(_save);
            GameSaveData data = _save.Data;
            MonetizationConfig monetization = database.monetization;

            var analytics = new MockAnalyticsService(monetization.logAnalyticsToConsole);
            ServiceLocator.Register<IAnalyticsService>(analytics);
            var ads = new MockAdsService(monetization.mockAdSeconds);
            ServiceLocator.Register<IAdsService>(ads);
            _tickables.Add(ads);
            ServiceLocator.Register<IIapService>(new MockIapService(data.meta, monetization.removeAdsProductId));

            var economy = new EconomyService(data.wallet);
            ServiceLocator.Register<IEconomyService>(economy);
            var settings = Add(new SettingsService(data.meta.settings));
            var boost = Add(new IncomeBoost(monetization, data.meta));

            var progression = Add(new ProgressionService(database, data.lots));
            var buildings = Add(new BuildingManager(database, economy, data.lots));
            var placement = Add(new PlacementService(database, buildings, data.stands, data.decorations));
            var stands = Add(new StandService(database, economy, buildings, data.stands, boost));
            var popularity = Add(new PopularityService(database, progression, stands));
            Add(new DecorationService(database, economy, progression, popularity, data.decorations)).AttachPlacement(placement);
            var navigation = Add(new WorldNavigation(database.world, database.visitors, placement));
            Add(new VisitorService(database, buildings, stands, popularity, navigation));
            var neighbours = Add(new NeighbourService(database, buildings, progression, data.neighbours));
            neighbours.AttachPlacement(placement);
            Add(new QuestService(database, neighbours, stands, economy, progression));

            // Meta systems read the restored island, so they come last.
            var estimator = Add(new IncomeEstimator(database, buildings, stands, popularity, neighbours));
            Add(new OfflineEarningsService(database, _save, clock, estimator, economy, ads, analytics));
            Add(new TutorialService(database.tutorial, data.meta, buildings));
            Add(new DailyRewardService(database.dailyRewards, data.meta, clock, estimator.IncomePerSecond, economy,
                boost, ads, analytics));
            AddOnline(data, placement, popularity, clock, estimator, economy, analytics);
            Add(new AudioService(database.sounds, settings, gameObject));
            Add(new AnalyticsReporter(analytics)).LogSessionStart(_save.LoadedFromDisk);

            // UI lives in the Beach scene and resolves services from the locator.

            // Saving is ticked last so an autosave captures this frame's changes.
            _tickables.Add(_save);
        }

        // Online seams with local mocks: identity, snapshot storage (demo friends seeded) and visiting.
        private void AddOnline(GameSaveData data, PlacementService placement, PopularityService popularity,
            ITimeProvider clock, IncomeEstimator estimator, IEconomyService economy, IAnalyticsService analytics)
        {
            var auth = new LocalAuthService(data.meta, database.online.defaultPlayerName);
            ServiceLocator.Register<IAuthService>(auth);
            var islands = new LocalIslandRepository(Application.persistentDataPath);
            ServiceLocator.Register<IIslandRepository>(islands);
            var social = new MockSocialService(DemoIslands.Friends(database.online));
            ServiceLocator.Register<ISocialService>(social);
            DemoIslands.Seed(database, islands, clock.UtcNow);

            IslandSnapshot CaptureHome() => IslandSnapshotFactory.Capture(auth.PlayerId, auth.DisplayName, data.lots,
                data.stands, data.decorations, placement, popularity.Value, clock.UtcNow);
            var ledger = new VisitBonusLedger(data.meta, database.online.maxBonusesPerDay);
            Add(new VisitService(database, auth, islands, social, CaptureHome, ledger, clock, estimator.IncomePerSecond,
                economy, analytics));
        }

        private T Add<T>(T service) where T : class
        {
            ServiceLocator.Register(service);
            if (service is ITickable tickable)
            {
                _tickables.Add(tickable);
            }

            if (service is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }

            return service;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickables[i].Tick(deltaTime);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _instance == this)
            {
                _save.Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (_instance == this)
            {
                _save.Save();
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            for (int i = 0; i < _disposables.Count; i++)
            {
                _disposables[i].Dispose();
            }

            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            _instance = null;
        }
    }
}
