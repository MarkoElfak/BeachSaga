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
        private SelectionService _selection;
        private CharacterView _character;

        private void Awake()
        {
            if (!GameBootstrap.IsReady)
            {
                // Started from the Beach scene in the editor: boot properly, Boot loads Beach again.
                SceneManager.LoadScene(SceneNames.Boot);
                return;
            }

            // Created before any view's Start so lots, stands and the HUD can all find it.
            _selection = new SelectionService(ServiceLocator.Get<PlacementService>());
            ServiceLocator.Register(_selection);
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
            worldBuilder.Build(database, _flats);
            TerrainShape ground = worldBuilder.Shape;
            ServiceLocator.Register(ground);
            _placement.AttachTerrain(ground);
            worldBuilder.ShowLandBoundary(_placement.UnlockedArea, MoreLandToUnlock());
            worldBuilder.ShowLand(ServiceLocator.Get<BuildingManager>().MainTier);
            Vector3 startFocus = LotPosition(0);
            Color seaColor = database.palette.GetColor(layout.seaPaletteKey);
            cameraController.Initialize(database.cameraConfig, _placement.UnlockedBounds, startFocus, seaColor);

            gestureSource.Initialize(database.input);
            tapRouter.Initialize(database.input);
            coinSpawner.Initialize(database.presentation, ServiceLocator.Get<IEconomyService>());

            SpawnLots();
            visitorViews.Initialize(database, ServiceLocator.Get<VisitorService>(), ground);
            ShowCharacter();
            ShowStaff(database, ground);
            decorations.Initialize(database, ServiceLocator.Get<DecorationService>(), _placement, ground);
            moveController.Initialize(_placement, database.input);
            RefreshPlacement(false);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Subscribe<AreaUnlocked>(OnAreaUnlocked);
            EventBus.Subscribe<LandOpened>(OnLandOpened);
            EventBus.Subscribe<CharacterJoined>(OnCharacterJoined);
        }

        private void OnDestroy()
        {
            _selection?.Dispose();
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Unsubscribe<AreaUnlocked>(OnAreaUnlocked);
            EventBus.Unsubscribe<LandOpened>(OnLandOpened);
            EventBus.Unsubscribe<CharacterJoined>(OnCharacterJoined);
        }

        private void OnObjectMoved(ObjectMoved evt) => RefreshPlacement(evt.Kind != PlaceableKind.Decoration);

        // Ground levelling follows buildings and stands; scenery hides under every object and stand queue.
        private void RefreshPlacement(bool lotsChanged)
        {
            _placement.GetFlatRects(_flats);
            _placement.GetOccupiedRects(_occupied);
            worldBuilder.RefreshPlacement(_flats, _occupied, lotsChanged);
        }

        // An upgrade opened more land: move the boundary fence; the camera roams it and full zoom-out shows it all.
        private void OnAreaUnlocked(AreaUnlocked evt)
        {
            worldBuilder.ShowLandBoundary(_placement.UnlockedArea, MoreLandToUnlock());
            cameraController.SetLimits(evt.Area);
        }

        // The fog over the new piece rolls away while the camera flies over it.
        private void OnLandOpened(LandOpened evt)
        {
            worldBuilder.OpenLand(evt.Piece);
            cameraController.FlyToArea(evt.Area);
        }

        private void OnCharacterJoined(CharacterJoined evt) => ShowCharacter();

        // The player's character walks among the visitors once a culture (and with it a character) is picked.
        private void ShowCharacter()
        {
            var characters = ServiceLocator.Get<CharacterService>();
            if (_character != null || !characters.HasJoined || characters.Character.prefab == null) return;
            _character = Instantiate(characters.Character.prefab, visitorViews.transform, false).GetComponent<CharacterView>();
            _character.Bind(characters, ServiceLocator.Get<StandService>(), ServiceLocator.Get<GameDatabase>().presentation,
                ServiceLocator.Get<TerrainShape>());
        }

        // Workers and job seekers walk among the visitors too.
        private void ShowStaff(GameDatabase database, TerrainShape ground)
        {
            var layer = new GameObject("Staff").AddComponent<WorkerViewLayer>();
            layer.transform.SetParent(visitorViews.transform, false);
            layer.Initialize(database, ServiceLocator.Get<StaffService>(), ServiceLocator.Get<StandService>(), ground);
        }

        private bool MoreLandToUnlock()
        {
            int tier = ServiceLocator.Get<BuildingManager>().MainTier;
            return _placement.AreaForTier(tier) != _placement.AreaForTier(ServiceLocator.Get<GameDatabase>().MaxTier);
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
