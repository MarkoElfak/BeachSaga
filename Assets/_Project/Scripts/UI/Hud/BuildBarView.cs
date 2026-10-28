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
    /// <summary>
    /// Bottom bar: tabs (Stands, Food, Services, Decor) over a row of the tab's products (other countries' places
    /// hidden) or decorations, and the main building's upgrade button.
    /// </summary>
    public sealed class BuildBarView : MonoBehaviour
    {
        public const int DecorTab = 3;

        [SerializeField] private ProductButtonView productButtonPrefab = null;
        [SerializeField] private DecorButtonView decorTilePrefab = null;
        [SerializeField] private Transform productRow = null;
        [SerializeField] private Button[] tabButtons = new Button[0];
        [SerializeField] private Color tabColor = Color.gray;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Button buildingButton = null;
        [SerializeField] private TMP_Text buildingLabel = null;

        private readonly List<ProductButtonView> _buttons = new List<ProductButtonView>();
        private readonly List<DecorButtonView> _tiles = new List<DecorButtonView>();
        private BuildingManager _buildings;
        private GameDatabase _database;
        private StandService _stands;

        /// <summary>The tab shown: a BuildCategory, or DecorTab.</summary>
        public int Tab { get; private set; }

        public int TabCount => tabButtons.Length;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _database))
            {
                return; // scene is redirecting to Boot
            }

            _buildings = ServiceLocator.Get<BuildingManager>();
            _stands = ServiceLocator.Get<StandService>();
            var economy = ServiceLocator.Get<IEconomyService>();
            foreach (ProductDefinition product in _database.products)
            {
                ProductButtonView button = Instantiate(productButtonPrefab, productRow, false);
                button.Bind(product, _stands, economy);
                _buttons.Add(button);
            }

            var decorations = ServiceLocator.Get<DecorationService>();
            foreach (DecorationDefinition definition in _database.decorations ?? new DecorationDefinition[0])
            {
                DecorButtonView tile = Instantiate(decorTilePrefab, productRow, false);
                tile.Bind(definition, decorations, economy);
                _tiles.Add(tile);
            }

            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tab = i;
                tabButtons[i].onClick.AddListener(() => ShowTab(tab));
            }

            buildingButton.onClick.AddListener(OnBuildingClicked);
            Subscribe(true);
            ShowTab(0);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < tabButtons.Length; i++) tabButtons[i].onClick.RemoveAllListeners();
            buildingButton.onClick.RemoveListener(OnBuildingClicked);
            Subscribe(false);
        }

        /// <summary>Shows one tab's products (or the decorations) in the row.</summary>
        public void ShowTab(int tab)
        {
            Tab = Mathf.Clamp(tab, 0, Mathf.Max(0, tabButtons.Length - 1));
            for (int i = 0; i < tabButtons.Length; i++) tabButtons[i].targetGraphic.color = i == Tab ? activeTabColor : tabColor;
            Refresh();
            EventBus.Publish(new BuildTabChanged(Tab));
        }

        /// <summary>The tab a product is listed under.</summary>
        public static int TabOf(ProductDefinition product) => (int)product.category;

        private void Subscribe(bool on)
        {
            if (on)
            {
                EventBus.Subscribe<CoinsChanged>(OnChanged);
                EventBus.Subscribe<LotStateChanged>(OnChanged);
                EventBus.Subscribe<StandCreated>(OnChanged);
                EventBus.Subscribe<StandUpgraded>(OnChanged);
                EventBus.Subscribe<TierUpgraded>(OnChanged);
                EventBus.Subscribe<CultureChosen>(OnChanged);
                EventBus.Subscribe<DecorationPlaced>(OnChanged);
            }
            else
            {
                EventBus.Unsubscribe<CoinsChanged>(OnChanged);
                EventBus.Unsubscribe<LotStateChanged>(OnChanged);
                EventBus.Unsubscribe<StandCreated>(OnChanged);
                EventBus.Unsubscribe<StandUpgraded>(OnChanged);
                EventBus.Unsubscribe<TierUpgraded>(OnChanged);
                EventBus.Unsubscribe<CultureChosen>(OnChanged);
                EventBus.Unsubscribe<DecorationPlaced>(OnChanged);
            }
        }

        private void OnChanged<T>(T evt) where T : struct => Refresh();

        private void OnBuildingClicked()
        {
            EventBus.Publish(new UpgradePanelRequested(BuildingManager.MainLotIndex));
        }

        private void Refresh()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                ProductButtonView button = _buttons[i];
                bool shown = TabOf(button.Product) == Tab && _stands.IsAvailable(button.Product);
                if (button.gameObject.activeSelf != shown) button.gameObject.SetActive(shown);
                if (shown) button.Refresh();
            }

            for (int i = 0; i < _tiles.Count; i++)
            {
                bool shown = Tab == DecorTab;
                if (_tiles[i].gameObject.activeSelf != shown) _tiles[i].gameObject.SetActive(shown);
                if (shown) _tiles[i].Refresh();
            }

            RefreshBuilding();
        }

        private void RefreshBuilding()
        {
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
