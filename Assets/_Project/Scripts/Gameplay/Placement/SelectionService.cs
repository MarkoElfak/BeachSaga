using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The object the player tapped on their own beach (building, house, stand or decoration). The HUD shows
    /// rotate arrows above it. Tapping open ground, starting a move, opening a window or leaving clears it.
    /// </summary>
    public sealed class SelectionService : IDisposable
    {
        private static readonly List<Renderer> Renderers = new List<Renderer>();

        private readonly PlacementService _placement;
        private float _topHeight;

        public SelectionService(PlacementService placement)
        {
            _placement = placement;
            EventBus.Subscribe<GroundTapped>(OnGroundTapped);
            EventBus.Subscribe<MoveStarted>(OnMoveStarted);
            EventBus.Subscribe<ModalVisibilityChanged>(OnModal);
            EventBus.Subscribe<LeavingHome>(OnLeavingHome);
        }

        public bool HasSelection { get; private set; }
        public PlaceableId Selected { get; private set; }

        /// <summary>Lot whose upgrade panel the selection opens, or -1 (houses and decorations).</summary>
        public int UpgradeLot { get; private set; } = -1;

        /// <summary>World point just above the middle of the selected object.</summary>
        public Vector3 Top
        {
            get
            {
                Vector2 center = _placement.GetFootprint(Selected, _placement.GetPosition(Selected)).center;
                return new Vector3(center.x, _topHeight, center.y);
            }
        }

        /// <summary>Selects an object. Tapping the selected object again clears the selection.</summary>
        public void Select(PlaceableId id, float topHeight, int upgradeLot)
        {
            if (HasSelection && Selected.Equals(id))
            {
                Clear();
                return;
            }

            Selected = id;
            UpgradeLot = upgradeLot;
            _topHeight = topHeight;
            HasSelection = true;
            EventBus.Publish(new SelectionChanged(true));
        }

        public void Clear()
        {
            if (!HasSelection) return;
            HasSelection = false;
            UpgradeLot = -1;
            EventBus.Publish(new SelectionChanged(false));
        }

        /// <summary>Turns the selection one step (1 = clockwise, -1 = counter-clockwise).</summary>
        public bool Rotate(int direction)
        {
            if (!HasSelection) return false;
            if (_placement.TryRotate(Selected, direction)) return true;
            EventBus.Publish(new RotateBlocked());
            return false;
        }

        /// <summary>Highest point of everything drawn under root, in world space.</summary>
        public static float TopOf(Transform root)
        {
            root.GetComponentsInChildren(false, Renderers);
            float top = root.position.y;
            for (int i = 0; i < Renderers.Count; i++)
            {
                if (Renderers[i].enabled) top = Mathf.Max(top, Renderers[i].bounds.max.y);
            }

            Renderers.Clear();
            return top;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<GroundTapped>(OnGroundTapped);
            EventBus.Unsubscribe<MoveStarted>(OnMoveStarted);
            EventBus.Unsubscribe<ModalVisibilityChanged>(OnModal);
            EventBus.Unsubscribe<LeavingHome>(OnLeavingHome);
            HasSelection = false;
        }

        private void OnGroundTapped(GroundTapped evt) => Clear();
        private void OnMoveStarted(MoveStarted evt) => Clear();
        private void OnLeavingHome(LeavingHome evt) => Clear();

        private void OnModal(ModalVisibilityChanged evt)
        {
            if (evt.Open) Clear();
        }
    }
}
