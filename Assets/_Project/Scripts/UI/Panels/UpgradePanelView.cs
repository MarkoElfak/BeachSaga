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
    /// <summary>
    /// Modal upgrade panel. For a lot it upgrades the building; for a stand it levels the stand up and hires
    /// its manager. Opened from the selection's Upgrade button or the build bar.
    /// </summary>
    public sealed partial class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text title = null;
        [SerializeField] private TMP_Text body = null;
        [SerializeField] private Button upgradeButton = null;
        [SerializeField] private TMP_Text upgradeLabel = null;
        [SerializeField] private Button hireButton = null;
        [SerializeField] private TMP_Text hireLabel = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Button backdropButton = null;

        private readonly StringBuilder _text = new StringBuilder();
        private BuildingManager _buildings;
        private StandService _stands;
        private IEconomyService _economy;
        private GameDatabase _database;
        private int _lotIndex = -1;
        private int _standIndex = -1;

        public bool IsOpen => root.activeSelf;

        /// <summary>The stand shown, or null when the panel is about a building.</summary>
        public StandController Stand => _standIndex >= 0 && _standIndex < _stands.Count ? _stands.Get(_standIndex) : null;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _database))
            {
                return;
            }

            _buildings = ServiceLocator.Get<BuildingManager>();
            _stands = ServiceLocator.Get<StandService>();
            _economy = ServiceLocator.Get<IEconomyService>();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            hireButton.onClick.AddListener(OnHireClicked);
            closeButton.onClick.AddListener(Close);
            backdropButton.onClick.AddListener(Close);
            EventBus.Subscribe<UpgradePanelRequested>(OnRequested);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
        }

        private void OnDestroy()
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            hireButton.onClick.RemoveListener(OnHireClicked);
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
            _standIndex = evt.StandIndex;
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

        // A building upgrade starts construction, so the panel closes; a stand stays open for more levels.
        private void OnUpgradeClicked()
        {
            StandController stand = Stand;
            if (stand != null)
            {
                if (_stands.TryUpgradeStand(stand)) Refresh();
            }
            else if (_buildings.TryUpgrade(_lotIndex))
            {
                Close();
            }
        }

        private void OnHireClicked()
        {
            StandController stand = Stand;
            if (stand != null && _stands.TryHireManager(stand)) Refresh();
        }

        private void Refresh()
        {
            _text.Clear();
            StandController stand = Stand;
            hireButton.gameObject.SetActive(stand != null);
            if (stand != null) RefreshStand(stand);
            else RefreshLot();
            body.text = _text.ToString();
        }

        private void SetButton(string label, bool interactable)
        {
            upgradeLabel.text = label;
            upgradeButton.interactable = interactable;
        }
    }
}
