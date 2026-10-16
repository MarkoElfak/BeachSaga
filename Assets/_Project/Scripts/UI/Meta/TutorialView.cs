using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Tutorial banner plus a bobbing arrow that points at the current target: the lot, the stand
    /// (or its waiting coin) or the first product button. Hides itself once the tutorial is done.
    /// </summary>
    public sealed class TutorialView : MonoBehaviour
    {
        [SerializeField] private GameObject banner = null;
        [SerializeField] private TMP_Text message = null;
        [SerializeField] private RectTransform arrow = null;
        [SerializeField] private RectTransform layer = null;
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private CoinSpawner coinSpawner = null;
        [SerializeField] private BuildBarView buildBar = null;

        private TutorialService _tutorial;
        private WorldNavigation _navigation;
        private TutorialConfig _config;
        private PlacementService _placement;
        private RectTransform _firstProductButton;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _tutorial))
            {
                gameObject.SetActive(false);
                return;
            }

            var database = ServiceLocator.Get<GameDatabase>();
            _config = database.tutorial;
            _navigation = ServiceLocator.Get<WorldNavigation>();
            _placement = ServiceLocator.Get<PlacementService>();
            EventBus.Subscribe<TutorialStepChanged>(OnStepChanged);
            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TutorialStepChanged>(OnStepChanged);
        }

        private void OnStepChanged(TutorialStepChanged evt) => Refresh();

        private void Refresh()
        {
            bool active = _tutorial.IsActive;
            banner.SetActive(active);
            arrow.gameObject.SetActive(active);
            if (active)
            {
                message.text = _tutorial.Current.message;
            }
        }

        private void LateUpdate()
        {
            if (_tutorial == null || !_tutorial.IsActive)
            {
                return;
            }

            bool visible = TryGetScreenTarget(_tutorial.Current.target, out Vector2 screen);
            if (arrow.gameObject.activeSelf != visible) arrow.gameObject.SetActive(visible);
            if (!visible || !RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local))
            {
                return;
            }

            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * _config.arrowBobSpeed)) * _config.arrowBobPixels;
            arrow.anchoredPosition = local + new Vector2(0f, bob);
        }

        private bool TryGetScreenTarget(TutorialTarget target, out Vector2 screen)
        {
            switch (target)
            {
                case TutorialTarget.MainLot:
                    Vector2 lot = _placement.GetLotPosition(BuildingManager.MainLotIndex);
                    return WorldToScreen(new Vector3(lot.x, 0f, lot.y), out screen);
                case TutorialTarget.FirstStandOrCoin:
                    if (coinSpawner.TryGetWaitingCoin(out Vector3 coin)) return WorldToScreen(coin, out screen);
                    if (_navigation.TryGetStandPosition(0, out Vector3 stand)) return WorldToScreen(stand, out screen);
                    break;
                case TutorialTarget.FirstProductButton:
                    if (_firstProductButton == null)
                    {
                        ProductButtonView button = buildBar.GetComponentInChildren<ProductButtonView>();
                        _firstProductButton = button != null ? (RectTransform)button.transform : null;
                    }

                    if (_firstProductButton != null)
                    {
                        // Overlay canvas: a RectTransform's world position is already in screen pixels.
                        Vector3 corners = _firstProductButton.position;
                        screen = new Vector2(corners.x, corners.y + _firstProductButton.rect.height * _firstProductButton.lossyScale.y * 0.5f);
                        return true;
                    }

                    break;
            }

            screen = default;
            return false;
        }

        private bool WorldToScreen(Vector3 world, out Vector2 screen)
        {
            Vector3 point = worldCamera.WorldToScreenPoint(world + Vector3.up * _config.worldArrowHeight);
            screen = new Vector2(point.x, point.y);
            return point.z > 0f;
        }
    }
}
