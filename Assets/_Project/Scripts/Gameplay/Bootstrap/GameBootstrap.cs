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
        private void CreateServices()
        {
            ServiceLocator.Register(database);
            var clock = new SystemTimeProvider();
            ServiceLocator.Register<ITimeProvider>(clock);

            _save = new SaveService(Application.persistentDataPath, database.economy.autosaveSeconds,
                database.economy.startingCoins, clock);
            ServiceLocator.Register<ISaveService>(_save);
            GameSaveData data = _save.Data;

            var economy = new EconomyService(data.wallet);
            ServiceLocator.Register<IEconomyService>(economy);

            var progression = Add(new ProgressionService(database, data.lots));
            var buildings = Add(new BuildingManager(database, economy, data.lots));
            var stands = Add(new StandService(database, economy, buildings, data.stands));
            var popularity = Add(new PopularityService(database, progression, stands));
            Add(new DecorationService(database, economy, progression, popularity, data.decorations));
            var navigation = Add(new WorldNavigation(database.world, database.visitors, buildings));
            Add(new VisitorService(database, buildings, stands, popularity, navigation));
            var neighbours = Add(new NeighbourService(database, buildings, progression, data.neighbours));
            Add(new QuestService(database, neighbours, stands, economy, progression));

            // UI lives in the Beach scene and resolves services from the locator.

            // Saving is ticked last so an autosave captures this frame's changes.
            _tickables.Add(_save);
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
