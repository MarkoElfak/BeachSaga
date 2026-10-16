using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Modal shop listing every decoration; buying one places it on the beach automatically.</summary>
    public sealed class DecorPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private DecorButtonView buttonPrefab = null;
        [SerializeField] private Transform list = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Button backdropButton = null;

        private readonly List<DecorButtonView> _buttons = new List<DecorButtonView>();

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out GameDatabase database))
            {
                return;
            }

            var decorations = ServiceLocator.Get<DecorationService>();
            var economy = ServiceLocator.Get<IEconomyService>();
            foreach (DecorationDefinition definition in database.decorations ?? new DecorationDefinition[0])
            {
                DecorButtonView button = Instantiate(buttonPrefab, list, false);
                button.Bind(definition, decorations, economy);
                _buttons.Add(button);
            }

            closeButton.onClick.AddListener(Close);
            backdropButton.onClick.AddListener(Close);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Close);
            backdropButton.onClick.RemoveListener(Close);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
        }

        public void Open()
        {
            root.SetActive(true);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        private void OnCoinsChanged(CoinsChanged evt) => RefreshIfOpen();
        private void OnDecorationPlaced(DecorationPlaced evt) => RefreshIfOpen();
        private void OnTierUpgraded(TierUpgraded evt) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen) Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].Refresh();
        }
    }
}
