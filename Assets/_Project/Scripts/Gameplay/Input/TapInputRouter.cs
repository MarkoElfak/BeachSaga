using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Turns TapEvents into a physics raycast and calls ITappable on whatever was hit.</summary>
    public sealed class TapInputRouter : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private LayerMask tappableLayers = ~0;

        private float _maxDistance;

        public void Initialize(InputConfig config)
        {
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
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, tappableLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }

            ITappable tappable = hit.collider.GetComponentInParent<ITappable>();
            if (tappable != null)
            {
                tappable.OnTapped();
            }
        }
    }
}
