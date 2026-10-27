using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Turns TapEvents into a physics raycast and calls ITappable on whatever was hit (else GroundTapped). A second
    /// quick tap on open ground next to the first is a DoubleTapEvent instead, so tapping coins never zooms.
    /// </summary>
    public sealed class TapInputRouter : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private LayerMask tappableLayers = ~0;

        private InputConfig _config;
        private float _maxDistance;
        private float _lastGroundTap = float.NegativeInfinity;
        private Vector2 _lastGroundPosition;

        public void Initialize(InputConfig config)
        {
            _config = config;
            _maxDistance = config.raycastMaxDistance;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TapEvent>(OnTap);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TapEvent>(OnTap);
        }

        private void OnTap(TapEvent evt)
        {
            if (worldCamera == null || _maxDistance <= 0f)
            {
                return;
            }

            // Coins and stands are spawned or moved during Update; make sure physics sees where they are now.
            Physics.SyncTransforms();
            Ray ray = worldCamera.ScreenPointToRay(evt.ScreenPosition);
            ITappable tappable = null;
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, tappableLayers, QueryTriggerInteraction.Collide))
            {
                tappable = hit.collider.GetComponentInParent<ITappable>();
            }

            if (tappable != null)
            {
                _lastGroundTap = float.NegativeInfinity;
                tappable.OnTapped();
            }
            else if (IsSecondTap(evt.ScreenPosition))
            {
                _lastGroundTap = float.NegativeInfinity;
                EventBus.Publish(new DoubleTapEvent(evt.ScreenPosition));
            }
            else
            {
                _lastGroundTap = Time.unscaledTime;
                _lastGroundPosition = evt.ScreenPosition;
                EventBus.Publish(new GroundTapped());
            }
        }

        private bool IsSecondTap(Vector2 position)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : _config.fallbackDpi;
            float reach = _config.doubleTapMaxInches * dpi;
            return Time.unscaledTime - _lastGroundTap <= _config.doubleTapSeconds
                && (position - _lastGroundPosition).sqrMagnitude <= reach * reach;
        }
    }
}
