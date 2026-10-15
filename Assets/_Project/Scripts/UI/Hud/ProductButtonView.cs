using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// One product in the build bar. Locked until its tier is reached, then Unlock (cost), then
    /// stand upgrades (Lv N, cost = base * growth ^ level) up to the max level.
    /// </summary>
    public sealed class ProductButtonView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Image background = null;
        [SerializeField] private TMP_Text nameLabel = null;
        [SerializeField] private TMP_Text statusLabel = null;
        [SerializeField] private TMP_Text actionLabel = null;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockColor = Color.yellow;
        [SerializeField] private Color ownedColor = Color.white;

        private ProductDefinition _product;
        private StandService _stands;
        private IEconomyService _economy;

        public void Bind(ProductDefinition product, StandService stands, IEconomyService economy)
        {
            _product = product;
            _stands = stands;
            _economy = economy;
            nameLabel.text = product.displayName;
            button.onClick.AddListener(OnClick);
            Refresh();
        }

        public void Refresh()
        {
            StandController stand = _stands.FindByProduct(_product.id);
            if (stand != null)
            {
                bool max = _stands.IsMaxLevel(stand);
                long cost = _stands.GetUpgradeCost(stand);
                statusLabel.text = "Lv " + (stand.Model.level + 1);
                actionLabel.text = max ? "MAX" : "Up " + NumberFormatter.Format(cost);
                button.interactable = !max && _economy.CanAfford(cost);
                background.color = ownedColor;
            }
            else if (!_stands.IsTierReached(_product))
            {
                statusLabel.text = "Tier " + _product.requiredTier;
                actionLabel.text = "Locked";
                button.interactable = false;
                background.color = lockedColor;
            }
            else
            {
                statusLabel.text = "New!";
                actionLabel.text = "Unlock " + NumberFormatter.Format(_product.unlockCost);
                button.interactable = _stands.CanUnlock(_product);
                background.color = unlockColor;
            }
        }

        private void OnClick()
        {
            StandController stand = _stands.FindByProduct(_product.id);
            if (stand != null)
            {
                _stands.TryUpgradeStand(stand);
            }
            else
            {
                _stands.TryUnlockProduct(_product);
            }
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}
