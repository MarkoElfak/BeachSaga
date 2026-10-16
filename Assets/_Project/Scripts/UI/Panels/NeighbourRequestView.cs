using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Small card asking the player to welcome a visitor as a neighbour.</summary>
    public sealed class NeighbourRequestView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text message = null;
        [SerializeField] private Button acceptButton = null;
        [SerializeField] private Button declineButton = null;

        private NeighbourService _neighbours;

        public bool IsOpen => root.activeSelf;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _neighbours))
            {
                return;
            }

            acceptButton.onClick.AddListener(OnAccept);
            declineButton.onClick.AddListener(OnDecline);
            EventBus.Subscribe<NeighbourRequested>(OnRequested);
            EventBus.Subscribe<NeighbourRequestClosed>(OnClosed);
        }

        private void OnDestroy()
        {
            acceptButton.onClick.RemoveListener(OnAccept);
            declineButton.onClick.RemoveListener(OnDecline);
            EventBus.Unsubscribe<NeighbourRequested>(OnRequested);
            EventBus.Unsubscribe<NeighbourRequestClosed>(OnClosed);
        }

        private void OnRequested(NeighbourRequested evt)
        {
            message.text = $"{evt.Name} loves your beach and wants to move in!";
            root.SetActive(true);
        }

        private void OnClosed(NeighbourRequestClosed evt) => root.SetActive(false);

        private void OnAccept() => _neighbours.Accept();

        private void OnDecline() => _neighbours.Decline();
    }
}
