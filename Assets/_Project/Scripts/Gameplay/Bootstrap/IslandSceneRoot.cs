using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Scene-level wiring for the Island scene: shows VisitService.Current read-only. Same terrain, props and
    /// camera as the Beach, plus a simulated crowd; there is no build bar, move controller or lot logic, so
    /// nothing can be edited. Pressing Play here redirects to Boot.
    /// </summary>
    public sealed class IslandSceneRoot : MonoBehaviour
    {
        [SerializeField] private BeachWorldBuilder worldBuilder = null;
        [SerializeField] private TopDownCameraController cameraController = null;
        [SerializeField] private PointerGestureSource gestureSource = null;
        [SerializeField] private TapInputRouter tapRouter = null;
        [SerializeField] private IslandPropSpawner props = null;
        [SerializeField] private VisitCrowd crowd = null;

        private readonly List<Rect> _flats = new List<Rect>();
        private readonly List<Rect> _occupied = new List<Rect>();
        private IslandLayout _layout;
        private WorldNavigation _navigation;

        /// <summary>The visited island's read-only layout (tests).</summary>
        public IslandLayout Layout => _layout;

        private void Awake()
        {
            if (!GameBootstrap.IsReady) SceneManager.LoadScene(SceneNames.Boot);
        }

        private void Start()
        {
            if (!GameBootstrap.IsReady) return;
            IslandSnapshot snapshot = ServiceLocator.Get<VisitService>().Current;
            if (snapshot == null)
            {
                SceneManager.LoadScene(SceneNames.Beach); // nothing to visit
                return;
            }

            GameDatabase database = ServiceLocator.Get<GameDatabase>();
            _layout = IslandLayout.FromSnapshot(database, snapshot);
            PlacementService placement = _layout.Placement;
            placement.GetFlatRects(_flats);
            worldBuilder.Build(database, _flats);
            TerrainShape ground = worldBuilder.Shape;
            placement.AttachTerrain(ground);
            bool moreLand = placement.AreaForTier(snapshot.tier) != placement.AreaForTier(database.MaxTier);
            worldBuilder.ShowLandBoundary(placement.UnlockedArea, moreLand);
            placement.GetOccupiedRects(_occupied);
            worldBuilder.RefreshPlacement(_flats, _occupied, false);

            Vector2 focus = placement.GetLotPosition(BuildingManager.MainLotIndex);
            TierDefinition tier = database.GetTier(Mathf.Max(1, snapshot.tier));
            Color sea = database.palette.GetColor(database.world.seaPaletteKey);
            cameraController.Initialize(database.cameraConfig, placement.UnlockedArea, tier != null ? tier.maxZoomDistance : 0f,
                new Vector3(focus.x, 0f, focus.y), sea);
            gestureSource.Initialize(database.input);
            tapRouter.Initialize(database.input);

            ServiceLocator.TryGet(out CultureService cultures);
            props.Spawn(database, _layout, ground, cultures, cultures?.Find(snapshot.cultureId));
            _navigation = new WorldNavigation(database.world, database.visitors, placement);
            crowd.Initialize(database, snapshot.tier, _navigation, _layout, ground);
        }

        private void OnDestroy()
        {
            _navigation?.Dispose();
            _layout?.Dispose();
        }
    }
}
