using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// City-builder style moving: long-press a building or decoration to lift it, drag it around (snapped to
    /// a grid, camera scrolls at the screen edge), release to drop. It only moves if the footprint is free;
    /// otherwise it slides back to where it was.
    /// </summary>
    public sealed class MoveController : MonoBehaviour
    {
        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);

        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private PointerGestureSource gestures = null;
        [SerializeField] private PlacementIndicator indicator = null;

        private PlacementService _placement;
        private PlacementConfig _config;
        private TerrainShape _terrain;
        private float _raycastDistance;

        private IMovable _active;
        private Vector3 _originalPosition;
        private Vector2 _grabOffset;
        private Vector2 _pointer;
        private Vector2 _target;
        private bool _valid;

        public bool IsMoving => _active != null;

        /// <summary>Snapped spot the held object would drop on, and whether that spot is free.</summary>
        public Vector2 Target => _target;
        public bool TargetIsValid => _valid;

        public void Initialize(PlacementService placement, InputConfig input)
        {
            _placement = placement;
            _config = placement.Config;
            _raycastDistance = input.raycastMaxDistance;
            ServiceLocator.TryGet(out _terrain);
            indicator.Hide();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<HoldStarted>(OnHoldStarted);
            EventBus.Subscribe<CapturedPointerMoved>(OnPointerMoved);
            EventBus.Subscribe<CapturedPointerReleased>(OnPointerReleased);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HoldStarted>(OnHoldStarted);
            EventBus.Unsubscribe<CapturedPointerMoved>(OnPointerMoved);
            EventBus.Unsubscribe<CapturedPointerReleased>(OnPointerReleased);
        }

        /// <summary>Starts moving whatever movable object is under the screen point. Returns false if none.</summary>
        public bool TryBegin(Vector2 screenPosition)
        {
            if (_placement == null || _active != null) return false;

            Physics.SyncTransforms();
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, ~0, QueryTriggerInteraction.Collide)) return false;

            IMovable movable = hit.collider.GetComponentInParent<IMovable>();
            if (movable == null || !movable.CanMoveNow || !TryGroundPoint(screenPosition, out Vector2 grabbed)) return false;

            _active = movable;
            _originalPosition = movable.MoveRoot.position;
            _grabOffset = _placement.GetPosition(movable.Placeable) - grabbed;
            _pointer = screenPosition;
            EventBus.Publish(new MoveStarted());
            UpdateTarget();
            return true;
        }

        /// <summary>Moves the held object to follow a screen point (also used by tests).</summary>
        public void DragTo(Vector2 screenPosition)
        {
            if (_active == null) return;
            _pointer = screenPosition;
            UpdateTarget();
        }

        /// <summary>Drops the held object: commits when the spot is free, otherwise puts it back.</summary>
        public bool Drop()
        {
            if (_active == null) return false;

            PlaceableId id = _active.Placeable;
            bool moved = _valid && _target != _placement.GetPosition(id);
            if (moved)
            {
                _placement.Commit(id, _target); // views snap themselves to the committed position
            }
            else
            {
                _active.MoveRoot.position = _originalPosition;
            }

            _active = null;
            indicator.Hide();
            EventBus.Publish(new MoveEnded(moved));
            return moved;
        }

        private void OnHoldStarted(HoldStarted evt)
        {
            if (TryBegin(evt.ScreenPosition)) gestures.CapturePointer();
        }

        private void OnPointerMoved(CapturedPointerMoved evt) => DragTo(evt.ScreenPosition);

        private void OnPointerReleased(CapturedPointerReleased evt) => Drop();

        private void Update()
        {
            if (_active == null || _config.edgePanZone <= 0f) return;

            // Scroll the camera while the finger rests near a screen edge, so objects can travel far.
            Vector2 size = new Vector2(Screen.width, Screen.height);
            Vector2 zone = size * _config.edgePanZone;
            Vector2 push = Vector2.zero;
            if (_pointer.x < zone.x) push.x = -1f; else if (_pointer.x > size.x - zone.x) push.x = 1f;
            if (_pointer.y < zone.y) push.y = -1f; else if (_pointer.y > size.y - zone.y) push.y = 1f;
            if (push == Vector2.zero) return;

            Vector2 center = size * 0.5f;
            Vector2 step = push * (_config.edgePanSpeed * Time.unscaledDeltaTime);
            EventBus.Publish(new DragEvent(center, center - step));
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            if (!TryGroundPoint(_pointer, out Vector2 ground)) return;

            PlaceableId id = _active.Placeable;
            _target = PlacementRules.Snap(ground + _grabOffset, _config.gridSize);
            _valid = _placement.CanPlace(id, _target);

            float baseHeight = id.IsLot || _terrain == null ? 0f : _terrain.Height(_target.x, _target.y);
            _active.MoveRoot.position = new Vector3(_target.x, baseHeight + _config.liftHeight, _target.y);
            indicator.Show(_placement.GetFootprint(id, _target), baseHeight, _valid);
        }

        private bool TryGroundPoint(Vector2 screen, out Vector2 point)
        {
            Ray ray = worldCamera.ScreenPointToRay(screen);
            if (Ground.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                point = new Vector2(hit.x, hit.z);
                return true;
            }

            point = default;
            return false;
        }
    }
}
