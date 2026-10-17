using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Coin readout in the top HUD; it gives a small bounce whenever the balance goes up.</summary>
    public sealed class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label = null;
        [SerializeField, Range(0f, 1f)] private float bounce = 0.12f;
        [SerializeField, Min(0.05f)] private float bounceSeconds = 0.3f;

        private long _shown = -1;

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
            if (_shown >= 0 && evt.Coins > _shown) ScalePunch.On(label, bounce, bounceSeconds);
            Show(evt.Coins);
        }

        // Only runs when the balance changes, so the string allocation is not a per-frame cost.
        private void Show(long coins)
        {
            _shown = coins;
            label.text = NumberFormatter.Format(coins);
        }
    }
}
