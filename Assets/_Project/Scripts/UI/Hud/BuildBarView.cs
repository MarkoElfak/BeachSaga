using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Bottom bar: one button per product, the main building's upgrade button and the decorations shop.</summary>
    public sealed class BuildBarView : MonoBehaviour
    {
        [SerializeField] private ProductButtonView productButtonPrefab = null;
        [SerializeField] private Transform productRow = null;
        [SerializeField] private Button buildingButton = null;
        [SerializeField] private TMP_Text buildingLabel = null;
        [SerializeField] private Button decorButton = null;
        [SerializeField] private DecorPanelView decorPanel = null;

        private readonly List<ProductButtonView> _buttons = new List<ProductButtonView>();
        private BuildingManager _buildings;
        private GameDatabase _database;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _database))
            {
                return; // scene is redirecting to Boot
            }

            _buildings = ServiceLocator.Get<BuildingManager>();
            var stands = ServiceLocator.Get<StandService>();
            var economy = ServiceLocator.Get<IEconomyService>();
            foreach (ProductDefinition product in _database.products)
            {
                ProductButtonView button = Instantiate(productButtonPrefab, productRow, false);
                button.Bind(product, stands, economy);
                _buttons.Add(button);
            }

            buildingButton.onClick.AddListener(OnBuildingClicked);
            decorButton.onClick.AddListener(decorPanel.Open);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            Refresh();
        }

        private void OnDestroy()
        {
            buildingButton.onClick.RemoveListener(OnBuildingClicked);
            if (decorPanel != null) decorButton.onClick.RemoveListener(decorPanel.Open);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
        }

        private void OnCoinsChanged(CoinsChanged evt) => Refresh();
        private void OnLotStateChanged(LotStateChanged evt) => Refresh();
        private void OnStandCreated(StandCreated evt) => Refresh();
        private void OnStandUpgraded(StandUpgraded evt) => Refresh();
        private void OnTierUpgraded(TierUpgraded evt) => Refresh();

        private void OnBuildingClicked()
        {
            EventBus.Publish(new UpgradePanelRequested(BuildingManager.MainLotIndex));
        }

        private void Refresh()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Refresh();
            }

            LotModel lot = _buildings.GetLot(BuildingManager.MainLotIndex);
            TierDefinition next = _buildings.GetNextTier(BuildingManager.MainLotIndex);
            buildingButton.interactable = lot.state == LotState.Built;
            if (lot.state == LotState.Empty)
            {
                buildingLabel.text = "Tap the empty lot to build";
            }
            else if (lot.state == LotState.Building)
            {
                buildingLabel.text = "Building...";
            }
            else if (next == null)
            {
                buildingLabel.text = _database.GetTier(lot.tier).displayName + " (max for now)";
            }
            else
            {
                buildingLabel.text = $"Upgrade to {next.displayName}  {NumberFormatter.Format(next.upgradeCost)}";
            }
        }
    }
}
