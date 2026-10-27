using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    // Land that grows with the main building: the rope fence around the open mainland, fog over locked pieces
    // (palms there stay hidden) and island boardwalks, which are built once their island opens.
    public sealed partial class BeachWorldBuilder
    {
        private readonly List<ScatterRenderer> _boardwalks = new List<ScatterRenderer>();
        private readonly List<int> _boardwalkPieces = new List<int>();
        private LandFence _fence;
        private LandFog _fog;

        public LandFog Fog => _fog;

        /// <summary>Fences the edge of the open mainland (no fence once all of it is open).</summary>
        public void ShowLandBoundary(Rect area, bool moreToUnlock)
        {
            if (_fence != null) _fence.Show(area, moreToUnlock);
        }

        /// <summary>Fog and signs over every piece still locked at this tier; boardwalks of open islands.</summary>
        public void ShowLand(int tier)
        {
            MapConfig map = _database.map;
            _fog.Build(_database, Shape, tier);
            for (int i = 0; i < _boardwalkPieces.Count; i++)
            {
                _boardwalks[i].gameObject.SetActive(LandMap.IsOpen(map.pieces[_boardwalkPieces[i]], tier));
            }

            RefreshPalms();
        }

        /// <summary>A piece opened: its fog rolls away, its palms and boardwalk appear.</summary>
        public void OpenLand(int piece)
        {
            _fog.Reveal(piece);
            for (int i = 0; i < _boardwalkPieces.Count; i++)
            {
                if (_boardwalkPieces[i] == piece) _boardwalks[i].gameObject.SetActive(true);
            }

            RefreshPalms();
        }

        /// <summary>The piece's boardwalk is built and showing (tests).</summary>
        public bool IsBoardwalkShown(int piece)
        {
            int at = _boardwalkPieces.IndexOf(piece);
            return at >= 0 && _boardwalks[at].gameObject.activeInHierarchy;
        }

        private void CreateLandLayers(Transform root, TerrainConfig config)
        {
            _fence = new GameObject("LandFence").AddComponent<LandFence>();
            _fence.transform.SetParent(root, false);
            _fence.Initialize(Shape, config);
            _fog = new GameObject("LandFog").AddComponent<LandFog>();
            _fog.transform.SetParent(root, false);

            MapConfig map = _database.map;
            if (!LandMap.HasPieces(map) || map.boardwalkMaterial == null || map.boardwalkMesh == null) return;
            var matrices = new List<Matrix4x4>();
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (!LandMap.HasBoardwalk(map.pieces[i])) continue;
                matrices.Clear();
                BoardwalkBuilder.Add(map, map.pieces[i], Shape, matrices);
                var walk = new GameObject($"Boardwalk {map.pieces[i].id}").AddComponent<ScatterRenderer>();
                walk.transform.SetParent(root, false);
                walk.Setup(map.boardwalkMesh, map.boardwalkMaterial, matrices, true);
                _boardwalks.Add(walk);
                _boardwalkPieces.Add(i);
            }
        }

        private bool IsUnderFog(Vector3 point) => _fog != null && _fog.Covers(point);

        private bool IsTowelArea(Vector2 point)
        {
            if (_layout.relaxArea.Contains(point)) return true;
            MapConfig map = _database.map;
            if (!LandMap.HasPieces(map)) return false;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (map.pieces[i].towelArea.Contains(point)) return true;
            }

            return false;
        }
    }
}
