using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Turning objects in place: PlacementConfig.rotationSteps steps per full circle. The turned footprint's
    /// bounding box must be free, just like a move; a step that does not fit is skipped. The yaw is saved in the models.
    /// </summary>
    public sealed partial class PlacementService
    {
        public float RotationStepDegrees => 360f / Mathf.Max(1, _config.rotationSteps);

        /// <summary>Yaw in degrees (0 = as built).</summary>
        public float GetYaw(PlaceableId id)
        {
            switch (id.Kind)
            {
                case PlaceableKind.Lot: return _buildings.GetLot(id.Index).yaw;
                case PlaceableKind.Stand: return id.Index >= 0 && id.Index < _stands.Count ? _stands[id.Index].yaw : 0f;
                default: return id.Index >= 0 && id.Index < _decorations.Count ? _decorations[id.Index].yaw : 0f;
            }
        }

        /// <summary>The yaw 'steps' steps clockwise (positive) or counter-clockwise (negative) from now.</summary>
        public float NextYaw(PlaceableId id, int steps)
        {
            float step = RotationStepDegrees;
            float yaw = Mathf.Round((GetYaw(id) + steps * step) / step) * step;
            return Mathf.Repeat(yaw, 360f);
        }

        /// <summary>The first yaw in that direction where the object fits (blocked steps are skipped).</summary>
        public bool TryGetNextYaw(PlaceableId id, int direction, out float yaw)
        {
            yaw = GetYaw(id);
            if (!IsPresent(id)) return false;
            Vector2 at = GetPosition(id);
            int steps = Mathf.Max(1, _config.rotationSteps);
            for (int i = 1; i < steps; i++)
            {
                float candidate = NextYaw(id, direction * i);
                if (!CanPlace(id, at, candidate)) continue;
                yaw = candidate;
                return true;
            }

            return false;
        }

        public bool CanRotate(PlaceableId id, int direction) => TryGetNextYaw(id, direction, out _);

        /// <summary>
        /// Turns an object one step, or further when that step does not fit; false when no other direction fits.
        /// Turning the main building leaves its stands where they are (pinned first, as for a move).
        /// </summary>
        public bool TryRotate(PlaceableId id, int direction)
        {
            if (!TryGetNextYaw(id, direction, out float yaw)) return false;
            if (id.IsLot && !BuildingManager.IsHouseLot(id.Index)) PinStands(id.Index);
            switch (id.Kind)
            {
                case PlaceableKind.Lot: _buildings.GetLot(id.Index).yaw = yaw; break;
                case PlaceableKind.Stand: _stands[id.Index].yaw = yaw; break;
                default: _decorations[id.Index].yaw = yaw; break;
            }

            EventBus.Publish(new ObjectMoved(id.Kind, id.Index));
            return true;
        }

        /// <summary>Rotation for an object's view.</summary>
        public Quaternion GetRotation(PlaceableId id) => Quaternion.Euler(0f, GetYaw(id), 0f);
    }
}
