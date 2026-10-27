using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Floats over the selected object: left/right arrows turn it one step, Upgrade (main building and stands
    /// only) opens the upgrade panel for it, Play (stands with a mini-game) opens its mini-game and Work here
    /// (other stands than the character's) sends the player's character there. Hidden while nothing is selected.
    /// </summary>
    public sealed class SelectionArrowsView : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private RectTransform area = null;
        [SerializeField] private RectTransform bar = null;
        [SerializeField] private Button leftButton = null;
        [SerializeField] private Button rightButton = null;
        [SerializeField] private Button upgradeButton = null;
        [SerializeField] private Button playButton = null;
        [SerializeField] private Button workButton = null;
        [SerializeField] private RawImage workPortrait = null;
        [Tooltip("Work here's height in the bar, and its height when Play is hidden.")]
        [SerializeField] private Vector2 workHeights = new Vector2(380f, 265f);
        [Tooltip("Gap in canvas units between the top of the object and the arrows.")]
        [SerializeField] private float lift = 20f;

        private SelectionService _selection;
        private MiniGameService _miniGames;
        private StandService _stands;
        private CharacterService _characters;

        private void Start()
        {
            bar.gameObject.SetActive(false);
            if (!ServiceLocator.TryGet(out _selection)) return;
            leftButton.onClick.AddListener(() => _selection.Rotate(-1));
            rightButton.onClick.AddListener(() => _selection.Rotate(1));
            upgradeButton.onClick.AddListener(OnUpgrade);
            playButton.onClick.AddListener(OnPlay);
            workButton.onClick.AddListener(OnWork);
            ServiceLocator.TryGet(out _miniGames);
            ServiceLocator.TryGet(out _stands);
            ServiceLocator.TryGet(out _characters);
            EventBus.Subscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.HasSelection)
            {
                upgradeButton.gameObject.SetActive(_selection.UpgradeLot >= 0);
                bool play = SelectedStand() >= 0;
                playButton.gameObject.SetActive(play);
                ShowWork(play);
            }

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

        private void OnPlay()
        {
            int stand = SelectedStand();
            _selection.Clear();
            if (stand >= 0) EventBus.Publish(new MiniGameRequested(stand));
        }

        private void ShowWork(bool playShown)
        {
            PlaceableId selected = _selection.Selected;
            bool show = _characters != null && selected.Kind == PlaceableKind.Stand && _characters.CanWorkAt(selected.Index);
            workButton.gameObject.SetActive(show);
            if (!show) return;
            workPortrait.texture = _characters.Portrait(_characters.Character);
            var rect = (RectTransform)workButton.transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, playShown ? workHeights.x : workHeights.y);
        }

        private void OnWork()
        {
            PlaceableId selected = _selection.Selected;
            _selection.Clear();
            if (selected.Kind == PlaceableKind.Stand) _characters.TrySendTo(selected.Index);
        }

        // The selected stand when its product has a mini-game, else -1.
        private int SelectedStand()
        {
            PlaceableId selected = _selection.Selected;
            if (selected.Kind != PlaceableKind.Stand || _miniGames == null || _stands == null) return -1;
            if (selected.Index < 0 || selected.Index >= _stands.Count) return -1;
            return _miniGames.ForProduct(_stands.Get(selected.Index).Model.productId) != null ? selected.Index : -1;
        }
    }
}
