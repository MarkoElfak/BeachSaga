using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Floats over the selected object: left/right arrows turn it one step, and Upgrade (main building and
    /// stands only) opens the upgrade panel for it. Hidden while nothing is selected.
    /// </summary>
    public sealed class SelectionArrowsView : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private RectTransform area = null;
        [SerializeField] private RectTransform bar = null;
        [SerializeField] private Button leftButton = null;
        [SerializeField] private Button rightButton = null;
        [SerializeField] private Button upgradeButton = null;
        [Tooltip("Gap in canvas units between the top of the object and the arrows.")]
        [SerializeField] private float lift = 20f;

        private SelectionService _selection;

        private void Start()
        {
            bar.gameObject.SetActive(false);
            if (!ServiceLocator.TryGet(out _selection)) return;
            leftButton.onClick.AddListener(() => _selection.Rotate(-1));
            rightButton.onClick.AddListener(() => _selection.Rotate(1));
            upgradeButton.onClick.AddListener(OnUpgrade);
            EventBus.Subscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.HasSelection) upgradeButton.gameObject.SetActive(_selection.UpgradeLot >= 0);
            Follow();
        }

        private void LateUpdate() => Follow();

        // Keeps the bar just above the object on screen; hides it when nothing is selected or it is off camera.
        private void Follow()
        {
            bool show = _selection != null && _selection.HasSelection && worldCamera != null;
            if (show)
            {
                Vector3 screen = worldCamera.WorldToScreenPoint(_selection.Top);
                show = screen.z > 0f;
                if (show && RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out Vector2 local))
                {
                    bar.anchoredPosition = local + new Vector2(0f, lift);
                }
            }

            if (bar.gameObject.activeSelf != show) bar.gameObject.SetActive(show);
        }

        // A selected stand opens its own panel (level up, manager); the main building opens the building's.
        private void OnUpgrade()
        {
            int lot = _selection.UpgradeLot;
            PlaceableId selected = _selection.Selected;
            int stand = selected.Kind == PlaceableKind.Stand ? selected.Index : -1;
            _selection.Clear();
            if (lot >= 0) EventBus.Publish(new UpgradePanelRequested(lot, stand));
        }
    }
}
