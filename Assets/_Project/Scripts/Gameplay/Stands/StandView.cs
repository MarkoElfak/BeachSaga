using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shows a stand's serve progress and drops coins on sales. Tapping it opens the upgrade panel.
    /// Tells WorldNavigation where its queue starts so visitors can line up.
    /// </summary>
    public sealed class StandView : MonoBehaviour, ITappable
    {
        [SerializeField] private Transform coinAnchor = null;
        [SerializeField] private ProgressBarView progressBar = null;

        [Tooltip("First queue position; the line grows along this transform's forward axis.")]
        [SerializeField] private Transform queueAnchor = null;

        private CoinSpawner _coinSpawner;

        public StandController Stand { get; private set; }

        public void Bind(StandController stand, CoinSpawner coinSpawner)
        {
            Stand = stand;
            _coinSpawner = coinSpawner;
            progressBar.SetVisible(false);
            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
        }

        /// <summary>Call after the stand is moved into place.</summary>
        public void RegisterQueue()
        {
            if (Stand != null && queueAnchor != null && ServiceLocator.TryGet(out WorldNavigation navigation))
            {
                navigation.RegisterQueue(Stand.Index, queueAnchor.position, queueAnchor.forward);
            }
        }

        public void OnTapped()
        {
            if (Stand != null)
            {
                EventBus.Publish(new UpgradePanelRequested(Stand.Model.lotIndex));
            }
        }

        private void Update()
        {
            if (Stand == null)
            {
                return;
            }

            bool serving = Stand.IsServing;
            progressBar.SetVisible(serving);
            if (serving)
            {
                progressBar.SetProgress(Stand.ServeProgress);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
        }

        private void OnSaleCompleted(SaleCompleted evt)
        {
            if (Stand != null && evt.StandIndex == Stand.Index)
            {
                _coinSpawner.Spawn(Stand.Index, coinAnchor.position, evt.Amount);
            }
        }
    }
}
