using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Scene-level wiring for the Beach scene: builds the world, spawns lot views and initializes the
    /// camera, input and coin pool with data from the GameDatabase. Pressing Play here redirects to Boot.
    /// </summary>
    public sealed class BeachSceneRoot : MonoBehaviour
    {
        [SerializeField] private BeachWorldBuilder worldBuilder = null;
        [SerializeField] private TopDownCameraController cameraController = null;
        [SerializeField] private PointerGestureSource gestureSource = null;
        [SerializeField] private TapInputRouter tapRouter = null;
        [SerializeField] private CoinSpawner coinSpawner = null;
        [SerializeField] private VisitorViewPool visitorViews = null;
        [SerializeField] private DecorationLayer decorations = null;
        [SerializeField] private LotView lotPrefab = null;
        [SerializeField] private Transform lotsRoot = null;

        private void Awake()
        {
            if (!GameBootstrap.IsReady)
            {
                // Started from the Beach scene in the editor: boot properly, Boot loads Beach again.
                SceneManager.LoadScene(SceneNames.Boot);
            }
        }

        private void Start()
        {
            if (!GameBootstrap.IsReady)
            {
                return;
            }

            GameDatabase database = ServiceLocator.Get<GameDatabase>();
            WorldLayoutConfig layout = database.world;

            Rect landBounds = worldBuilder.Build(database);
            TerrainShape ground = worldBuilder.Shape;
            ServiceLocator.Register(ground);
            Vector3 startFocus = LotPosition(layout, 0);
            Color seaColor = database.palette.GetColor(layout.seaPaletteKey);
            cameraController.Initialize(database.cameraConfig, landBounds, startFocus, seaColor);

            gestureSource.Initialize(database.input);
            tapRouter.Initialize(database.input);
            coinSpawner.Initialize(database.presentation, ServiceLocator.Get<IEconomyService>());

            SpawnLots(layout);
            visitorViews.Initialize(database, ServiceLocator.Get<VisitorService>(), ground);
            decorations.Initialize(database, ServiceLocator.Get<DecorationService>(), ground);
        }

        private void SpawnLots(WorldLayoutConfig layout)
        {
            BuildingManager buildings = ServiceLocator.Get<BuildingManager>();
            for (int i = 0; i < buildings.LotCount; i++)
            {
                LotView lot = Instantiate(lotPrefab, lotsRoot, false);
                lot.name = $"Lot {i}";
                lot.transform.localPosition = LotPosition(layout, i);
                lot.Bind(i, coinSpawner);
            }
        }

        private static Vector3 LotPosition(WorldLayoutConfig layout, int index)
        {
            if (layout.lotPositions == null || index >= layout.lotPositions.Length)
            {
                return Vector3.zero;
            }

            Vector2 xz = layout.lotPositions[index];
            return new Vector3(xz.x, 0f, xz.y);
        }
    }
}
