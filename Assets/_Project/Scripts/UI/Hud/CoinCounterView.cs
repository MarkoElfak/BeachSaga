using BeachTycoon.Core;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Coin readout in the top HUD.</summary>
    public sealed class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label = null;

        private void OnEnable()
        {
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            Show(ServiceLocator.TryGet(out IEconomyService economy) ? economy.Coins : 0);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChanged evt)
        {
            Show(evt.Coins);
        }

        // Only runs when the balance changes, so the string allocation is not a per-frame cost.
        private void Show(long coins)
        {
            label.text = NumberFormatter.Format(coins);
        }
    }
}
