using System.Collections.Generic;
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
        [SerializeField] private MoveController moveController = null;

        private readonly List<Rect> _flats = new List<Rect>();
        private readonly List<Rect> _occupied = new List<Rect>();
        private PlacementService _placement;

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

            _placement = ServiceLocator.Get<PlacementService>();
            _placement.GetFlatRects(_flats);
            Rect landBounds = worldBuilder.Build(database, _flats);
            TerrainShape ground = worldBuilder.Shape;
            ServiceLocator.Register(ground);
            _placement.AttachTerrain(ground);
            Vector3 startFocus = LotPosition(0);
            Color seaColor = database.palette.GetColor(layout.seaPaletteKey);
            cameraController.Initialize(database.cameraConfig, landBounds, startFocus, seaColor);

            gestureSource.Initialize(database.input);
            tapRouter.Initialize(database.input);
            coinSpawner.Initialize(database.presentation, ServiceLocator.Get<IEconomyService>());

            SpawnLots();
            visitorViews.Initialize(database, ServiceLocator.Get<VisitorService>(), ground);
            decorations.Initialize(database, ServiceLocator.Get<DecorationService>(), _placement, ground);
            moveController.Initialize(_placement, database.input);
            RefreshPlacement(false);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
        }

        private void OnObjectMoved(ObjectMoved evt) => RefreshPlacement(evt.IsLot);

        // Ground flattening follows buildings (only lots change it); scenery hides under every object.
        private void RefreshPlacement(bool lotsChanged)
        {
            _placement.GetFlatRects(_flats);
            _placement.GetOccupiedRects(_occupied);
            worldBuilder.RefreshPlacement(_flats, _occupied, lotsChanged);
        }

        private void SpawnLots()
        {
            BuildingManager buildings = ServiceLocator.Get<BuildingManager>();
            for (int i = 0; i < buildings.LotCount; i++)
            {
                LotView lot = Instantiate(lotPrefab, lotsRoot, false);
                lot.name = $"Lot {i}";
                lot.transform.localPosition = LotPosition(i);
                lot.Bind(i, coinSpawner);
            }
        }

        private Vector3 LotPosition(int index)
        {
            Vector2 xz = _placement.GetLotPosition(index);
            return new Vector3(xz.x, 0f, xz.y);
        }
    }
}
