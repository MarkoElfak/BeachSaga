using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Displays one lot: the empty marker (main lot only), the construction crate, the building or house and its
    /// stands, dressed in the player's culture. Forwards taps (build, select, house info) and drops rent coins.
    /// Long-press moves the building alone: stands are separate objects (StandView) next to it. Holds no game state.
    /// </summary>
    public sealed class LotView : MonoBehaviour, ITappable, IMovable
    {
        private const int RentSourceOffset = 1000;
        private const float TapBoxHeight = 2.5f;

        [SerializeField] private GameObject emptyMarker = null;
        [SerializeField] private Transform constructionVisual = null;
        [SerializeField] private ProgressBarView progressBar = null;
        [SerializeField] private Transform buildingRoot = null;

        private readonly List<StandView> _standViews = new List<StandView>();
        private int _lotIndex = -1;
        private BuildingManager _buildings;
        private StandService _stands;
        private GameDatabase _database;
        private CoinSpawner _coinSpawner;
        private CultureService _culture;
        private BuildingView _buildingView;
        private int _shownTier;
        private bool _bound;

        private PlacementService _placement;

        private bool IsHouse => BuildingManager.IsHouseLot(_lotIndex);

        public PlaceableId Placeable => PlaceableId.Lot(_lotIndex);
        public bool CanMoveNow => _lotIndex >= 0 && _placement != null && _placement.IsPresent(Placeable);
        public Transform MoveRoot => transform;

        public void Bind(int lotIndex, CoinSpawner coinSpawner)
        {
            _lotIndex = lotIndex;
            _coinSpawner = coinSpawner;
            _buildings = ServiceLocator.Get<BuildingManager>();
            _stands = ServiceLocator.Get<StandService>();
            _database = ServiceLocator.Get<GameDatabase>();
            _placement = ServiceLocator.Get<PlacementService>();
            ServiceLocator.TryGet(out _culture);
            SnapToPlacement();

            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<RentDue>(OnRentDue);
            EventBus.Subscribe<CultureChosen>(OnCultureChosen);

            Refresh();
            for (int i = 0; i < _stands.Count; i++)
            {
                if (_stands.Get(i).Model.lotIndex == _lotIndex) SpawnStandView(i);
            }

            _bound = true;
        }

        // An empty main lot builds; a building or house is selected (rotate arrows, upgrade for the main one).
        public void OnTapped()
        {
            if (_lotIndex < 0) return;
            LotState state = _buildings.GetLot(_lotIndex).state;
            if (state == LotState.Empty)
            {
                if (!IsHouse) _buildings.TryBuild(_lotIndex);
                return;
            }

            if (IsHouse) EventBus.Publish(new HouseTapped(_lotIndex));
            float top = SelectionService.TopOf(state == LotState.Building ? constructionVisual : buildingRoot);
            ServiceLocator.Get<SelectionService>().Select(Placeable, top, IsHouse ? -1 : _lotIndex);
        }

        private void Update()
        {
            if (_lotIndex < 0) return;
            LotModel lot = _buildings.GetLot(_lotIndex);
            if (lot.state != LotState.Building) return;

            float progress = lot.BuildProgress;
            progressBar.SetProgress(progress);
            float scale = Mathf.Lerp(_database.presentation.constructionStartScale, 1f, progress);
            constructionVisual.localScale = new Vector3(scale, scale, scale);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<RentDue>(OnRentDue);
            EventBus.Unsubscribe<CultureChosen>(OnCultureChosen);
        }

        private void OnObjectMoved(ObjectMoved evt)
        {
            if (evt.Kind == PlaceableKind.Lot && evt.Index == _lotIndex) SnapToPlacement();
        }

        // Lots stand on levelled ground at height 0; only the building turns (the tap box follows its footprint).
        private void SnapToPlacement()
        {
            Vector2 position = _placement.GetLotPosition(_lotIndex);
            transform.position = new Vector3(position.x, 0f, position.y);
            buildingRoot.localRotation = _placement.GetRotation(Placeable);
            FitCollider();
        }

        // Taps and long-presses hit exactly the ground the building covers (it changes with the tier).
        private void FitCollider()
        {
            if (!TryGetComponent(out BoxCollider box)) return;
            box.enabled = _placement.IsPresent(Placeable); // empty house lots are not there yet
            Rect footprint = _placement.GetFootprint(Placeable, Vector2.zero);
            box.center = new Vector3(footprint.center.x, TapBoxHeight * 0.5f, footprint.center.y);
            box.size = new Vector3(footprint.width, TapBoxHeight, footprint.height);
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            if (evt.LotIndex == _lotIndex) Refresh();
        }

        // An older village picking its culture: the building is shown again, now in that culture.
        private void OnCultureChosen(CultureChosen evt)
        {
            _shownTier = -1;
            Refresh();
        }

        private void OnStandCreated(StandCreated evt)
        {
            if (evt.LotIndex == _lotIndex && SpawnStandView(evt.StandIndex) is StandView view) view.Punch();
        }

        private void Punch(Transform target) =>
            ScalePunch.On(target, _database.presentation.punchScale, _database.presentation.punchSeconds);

        private void OnRentDue(RentDue evt)
        {
            if (evt.LotIndex != _lotIndex) return;
            Vector3 position = _buildingView != null ? _buildingView.CoinPosition : transform.position + Vector3.up;
            _coinSpawner.Spawn(RentSourceOffset + _lotIndex, position, evt.Amount);
        }

        private void Refresh()
        {
            LotModel lot = _buildings.GetLot(_lotIndex);
            bool building = lot.state == LotState.Building;
            emptyMarker.SetActive(lot.state == LotState.Empty && !IsHouse);
            constructionVisual.gameObject.SetActive(building);
            progressBar.SetVisible(building);
            if (lot.tier != _shownTier) ShowBuilding(lot);
            FitCollider();
        }

        private void ShowBuilding(LotModel lot)
        {
            if (_buildingView != null)
            {
                Destroy(_buildingView.gameObject);
                _buildingView = null;
            }

            _shownTier = lot.tier;
            CultureDefinition culture = _culture?.Current;
            GameObject prefab = Resolve(IsHouse ? HousePrefab(lot.variant) : TierPrefab(lot.tier), culture);
            if (prefab == null || lot.tier == 0) return;

            GameObject instance = Instantiate(prefab, buildingRoot, false);
            _culture?.Dress(instance, culture, !IsHouse);
            _buildingView = instance.GetComponent<BuildingView>();
            if (_bound) Punch(instance.transform); // a building that just finished, not one loaded with the save
        }

        private GameObject Resolve(GameObject prefab, CultureDefinition culture) => _culture != null ? _culture.Resolve(prefab, culture) : prefab;

        private GameObject TierPrefab(int tier)
        {
            TierDefinition definition = _database.GetTier(tier);
            return definition != null ? definition.buildingPrefab : null;
        }

        private GameObject HousePrefab(int variant)
        {
            GameObject[] houses = _database.neighbours.housePrefabs;
            return houses != null && houses.Length > 0 ? houses[Mathf.Abs(variant) % houses.Length] : null;
        }

        // Stands are siblings of the lot (not children), so moving the building leaves them where they are.
        private StandView SpawnStandView(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            if (stand.Product.standPrefab == null)
            {
                Debug.LogWarning($"Product '{stand.Product.id}' has no stand prefab.", stand.Product);
                return null;
            }

            GameObject instance = Instantiate(Resolve(stand.Product.standPrefab, _culture?.Current), transform.parent, false);
            var view = instance.GetComponent<StandView>();
            view.Bind(stand, _coinSpawner);
            _standViews.Add(view);
            return view;
        }
    }
}
