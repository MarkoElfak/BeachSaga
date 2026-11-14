using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>One decoration in the build bar's Decor tab: name, popularity bonus, how many are placed, and the price.</summary>
    public sealed class DecorButtonView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private TMP_Text nameLabel = null;
        [SerializeField] private TMP_Text detailLabel = null;
        [SerializeField] private TMP_Text priceLabel = null;

        private DecorationDefinition _definition;
        private DecorationService _decorations;
        private IEconomyService _economy;

        public void Bind(DecorationDefinition definition, DecorationService decorations, IEconomyService economy)
        {
            _definition = definition;
            _decorations = decorations;
            _economy = economy;
            nameLabel.text = definition.displayName;
            button.onClick.AddListener(OnClick);
            Refresh();
        }

        public void Refresh()
        {
            int placed = _decorations.CountOf(_definition);
            detailLabel.text = $"+{_definition.popularity:0.##} pop.  {placed}/{_definition.maxCount}";
            if (!_decorations.IsUnlocked(_definition))
            {
                priceLabel.text = "Tier " + _definition.requiredTier;
            }
            else if (_decorations.IsSoldOut(_definition))
            {
                priceLabel.text = "Full";
            }
            else
            {
                priceLabel.text = NumberFormatter.Format(_definition.cost);
            }

            button.interactable = _decorations.CanBuy(_definition) && _economy.CanAfford(_definition.cost);
        }

        private void OnClick() => _decorations.TryBuy(_definition);

        private void OnDestroy() => button.onClick.RemoveListener(OnClick);
    }
}
