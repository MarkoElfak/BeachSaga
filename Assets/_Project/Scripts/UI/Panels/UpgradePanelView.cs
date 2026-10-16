using System.Text;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Modal panel for upgrading a lot's building. Opened by tapping the building, a stand or the build bar.</summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text title = null;
        [SerializeField] private TMP_Text body = null;
        [SerializeField] private Button upgradeButton = null;
        [SerializeField] private TMP_Text upgradeLabel = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Button backdropButton = null;

        private readonly StringBuilder _text = new StringBuilder();
        private BuildingManager _buildings;
        private IEconomyService _economy;
        private GameDatabase _database;
        private int _lotIndex = -1;

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _database))
            {
                return;
            }

            _buildings = ServiceLocator.Get<BuildingManager>();
            _economy = ServiceLocator.Get<IEconomyService>();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            closeButton.onClick.AddListener(Close);
            backdropButton.onClick.AddListener(Close);
            EventBus.Subscribe<UpgradePanelRequested>(OnRequested);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
        }

        private void OnDestroy()
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            closeButton.onClick.RemoveListener(Close);
            backdropButton.onClick.RemoveListener(Close);
            EventBus.Unsubscribe<UpgradePanelRequested>(OnRequested);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
        }

        public void Close() => root.SetActive(false);

        private void OnRequested(UpgradePanelRequested evt)
        {
            _lotIndex = evt.LotIndex;
            root.SetActive(true);
            Refresh();
        }

        private void OnCoinsChanged(CoinsChanged evt)
        {
            if (IsOpen) Refresh();
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            if (IsOpen && evt.LotIndex == _lotIndex) Refresh();
        }

        private void OnUpgradeClicked()
        {
            if (_buildings.TryUpgrade(_lotIndex))
            {
                Close();
            }
        }

        private void Refresh()
        {
            LotModel lot = _buildings.GetLot(_lotIndex);
            TierDefinition current = _database.GetTier(lot.tier);
            TierDefinition next = _buildings.GetNextTier(_lotIndex);
            string currentName = current != null ? current.displayName : "Empty lot";
            _text.Clear();

            if (lot.state == LotState.Building)
            {
                title.text = currentName;
                _text.Append("Construction in progress...");
                SetButton("Building", false);
            }
            else if (next == null)
            {
                title.text = currentName;
                _text.Append("This is the best building available for now.");
                SetButton("Max", false);
            }
            else
            {
                title.text = $"{currentName} to {next.displayName}";
                _text.Append("Visitors at once: ").Append(next.maxVisitors);
                if (current != null) _text.Append(" (now ").Append(current.maxVisitors).Append(')');
                _text.Append("\nPopularity bonus: +").Append(next.popularityBonus.ToString("0.##"));
                AppendUnlocks(next);
                SetButton("Upgrade  " + NumberFormatter.Format(next.upgradeCost), _economy.CanAfford(next.upgradeCost));
            }

            body.text = _text.ToString();
        }

        private void AppendUnlocks(TierDefinition tier)
        {
            if (tier.unlockedProducts == null || tier.unlockedProducts.Length == 0) return;
            _text.Append("\nUnlocks: ");
            for (int i = 0; i < tier.unlockedProducts.Length; i++)
            {
                if (i > 0) _text.Append(", ");
                _text.Append(tier.unlockedProducts[i] != null ? tier.unlockedProducts[i].displayName : "?");
            }
        }

        private void SetButton(string label, bool interactable)
        {
            upgradeLabel.text = label;
            upgradeButton.interactable = interactable;
        }
    }
}
