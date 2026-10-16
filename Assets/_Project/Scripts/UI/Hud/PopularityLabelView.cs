using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Popularity readout in the top HUD.</summary>
    public sealed class PopularityLabelView : MonoBehaviour
    {
        private const string Format = "Popularity {0:2}";

        [SerializeField] private TMP_Text label = null;

        private void OnEnable()
        {
            EventBus.Subscribe<PopularityChanged>(OnPopularityChanged);
            Show(ServiceLocator.TryGet(out PopularityService popularity) ? popularity.Value : 0f);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PopularityChanged>(OnPopularityChanged);
        }

        private void OnPopularityChanged(PopularityChanged evt) => Show(evt.Value);

        // TMP's SetText(format, float) formats without allocating.
        private void Show(float value) => label.SetText(Format, value);
    }
}
