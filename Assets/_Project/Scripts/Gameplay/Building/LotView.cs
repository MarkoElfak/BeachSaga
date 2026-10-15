using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Displays one lot: the empty marker (main lot only), the construction crate, the building or house and its
    /// stands. Forwards taps (build, upgrade panel, house info) and drops rent coins. Holds no game state.
    /// </summary>
    public sealed class LotView : MonoBehaviour, ITappable
    {
        private const int RentSourceOffset = 1000;

        [SerializeField] private GameObject emptyMarker = null;
        [SerializeField] private Transform constructionVisual = null;
        [SerializeField] private ProgressBarView progressBar = null;
        [SerializeField] private Transform buildingRoot = null;
        [SerializeField] private Transform standsRoot = null;

        private readonly List<StandView> _standViews = new List<StandView>();
        private int _lotIndex = -1;
        private BuildingManager _buildings;
        private StandService _stands;
        private GameDatabase _database;
        private CoinSpawner _coinSpawner;
        private BuildingView _buildingView;
        private int _shownTier;

        private bool IsHouse => BuildingManager.IsHouseLot(_lotIndex);

        public void Bind(int lotIndex, CoinSpawner coinSpawner)
        {
            _lotIndex = lotIndex;
            _coinSpawner = coinSpawner;
            _buildings = ServiceLocator.Get<BuildingManager>();
            _stands = ServiceLocator.Get<StandService>();
            _database = ServiceLocator.Get<GameDatabase>();

            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<RentDue>(OnRentDue);

            Refresh();
            for (int i = 0; i < _stands.Count; i++)
            {
                if (_stands.Get(i).Model.lotIndex == _lotIndex) SpawnStandView(i);
            }
        }

        public void OnTapped()
        {
            if (_lotIndex < 0) return;
            LotState state = _buildings.GetLot(_lotIndex).state;
            if (IsHouse)
            {
                if (state == LotState.Built) EventBus.Publish(new HouseTapped(_lotIndex));
            }
            else if (state == LotState.Empty)
            {
                _buildings.TryBuild(_lotIndex);
            }
            else
            {
                EventBus.Publish(new UpgradePanelRequested(_lotIndex));
            }
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
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<RentDue>(OnRentDue);
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            if (evt.LotIndex == _lotIndex) Refresh();
        }

        private void OnStandCreated(StandCreated evt)
        {
            if (evt.LotIndex != _lotIndex) return;
            Refresh(); // make sure the building (and its stand anchors) exists first
            SpawnStandView(evt.StandIndex);
        }

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
        }

        private void ShowBuilding(LotModel lot)
        {
            if (_buildingView != null)
            {
                Destroy(_buildingView.gameObject);
                _buildingView = null;
            }

            _shownTier = lot.tier;
            GameObject prefab = IsHouse ? HousePrefab(lot.variant) : TierPrefab(lot.tier);
            if (prefab == null || lot.tier == 0) return;

            GameObject instance = Instantiate(prefab, buildingRoot, false);
            _buildingView = instance.GetComponent<BuildingView>();
            for (int i = 0; i < _standViews.Count; i++) PlaceStand(_standViews[i]);
        }

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

        private void SpawnStandView(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            if (stand.Product.standPrefab == null)
            {
                Debug.LogWarning($"Product '{stand.Product.id}' has no stand prefab.", stand.Product);
                return;
            }

            GameObject instance = Instantiate(stand.Product.standPrefab, standsRoot, false);
            var view = instance.GetComponent<StandView>();
            view.Bind(stand, _coinSpawner);
            _standViews.Add(view);
            PlaceStand(view);
        }

        private void PlaceStand(StandView view)
        {
            Transform anchor = _buildingView != null
                ? _buildingView.GetStandAnchor(view.Stand.Model.slotIndex)
                : standsRoot;
            view.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            view.RegisterQueue();
        }
    }
}
