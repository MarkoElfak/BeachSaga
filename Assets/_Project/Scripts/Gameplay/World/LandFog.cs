using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Fog over every piece of the map that is still locked at the given tier (one LockedLandView each).</summary>
    public sealed class LandFog : MonoBehaviour
    {
        private readonly List<LockedLandView> _views = new List<LockedLandView>();
        private float _revealSeconds = 1f;

        /// <summary>Locked pieces that still have fog (pieces rolling away count until they finish).</summary>
        public int Count => _views.Count;

        public LockedLandView this[int index] => _views[index];

        public void Build(GameDatabase database, TerrainShape ground, int tier)
        {
            MapConfig map = database.map;
            if (!LandMap.HasPieces(map) || map.fogMaterial == null) return;
            _revealSeconds = map.revealSeconds;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (LandMap.IsOpen(map.pieces[i], tier)) continue;
                var view = new GameObject($"Fog {map.pieces[i].id}").AddComponent<LockedLandView>();
                view.transform.SetParent(transform, false);
                view.Build(database, i, ground);
                _views.Add(view);
            }
        }

        /// <summary>The piece opened: its fog rolls away. False when it had no fog.</summary>
        public bool Reveal(int piece)
        {
            LockedLandView view = Find(piece);
            if (view == null) return false;
            view.Reveal(_revealSeconds);
            return true;
        }

        public LockedLandView Find(int piece)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].Piece == piece) return _views[i];
            }

            return null;
        }

        /// <summary>The point is under fog that has not started rolling away (scenery there stays hidden).</summary>
        public bool Covers(Vector3 point)
        {
            var xz = new Vector2(point.x, point.z);
            for (int i = 0; i < _views.Count; i++)
            {
                if (!_views[i].IsOpening && _views[i].Area.Contains(xz)) return true;
            }

            return false;
        }
    }
}
