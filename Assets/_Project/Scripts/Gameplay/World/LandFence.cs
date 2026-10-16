using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Low rope fence along the edge of the land unlocked so far (sides and back; the sea side is open). Gaps are
    /// left where the footpath crosses and where the ground is under water. Drawn with GPU instancing.
    /// </summary>
    public sealed class LandFence : MonoBehaviour
    {
        private readonly List<Matrix4x4> _posts = new List<Matrix4x4>();
        private readonly List<Matrix4x4> _ropes = new List<Matrix4x4>();
        private ScatterRenderer _postRenderer;
        private ScatterRenderer _ropeRenderer;
        private TerrainShape _ground;
        private TerrainConfig _config;

        public void Initialize(TerrainShape ground, TerrainConfig config)
        {
            _ground = ground;
            _config = config;
            _postRenderer = CreateRenderer("FencePosts");
            _ropeRenderer = CreateRenderer("FenceRopes");
        }

        /// <summary>Rebuilds the fence around 'area'; pass visible = false to remove it (all land unlocked).</summary>
        public void Show(Rect area, bool visible)
        {
            _posts.Clear();
            _ropes.Clear();
            if (visible && _config != null && _config.fenceMesh != null)
            {
                Edge(new Vector2(area.xMin, area.yMax), new Vector2(area.xMin, area.yMin));
                Edge(new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin));
                Edge(new Vector2(area.xMax, area.yMin), new Vector2(area.xMax, area.yMax));
            }

            _postRenderer.gameObject.SetActive(_posts.Count > 0);
            _ropeRenderer.gameObject.SetActive(_ropes.Count > 0);
            if (_posts.Count > 0) _postRenderer.Setup(_config.fenceMesh, _config.fencePostMaterial, _posts, true);
            if (_ropes.Count > 0) _ropeRenderer.Setup(_config.fenceMesh, _config.fenceRopeMaterial, _ropes, false);
        }

        private void Edge(Vector2 from, Vector2 to)
        {
            int segments = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / _config.fencePostSpacing));
            Vector3 previous = default;
            bool previousOk = false;
            for (int i = 0; i <= segments; i++)
            {
                Vector2 xz = Vector2.Lerp(from, to, (float)i / segments);
                bool ok = IsFenceGround(xz);
                Vector3 ground = _ground.Ground(xz.x, xz.y);
                if (ok) AddPost(ground);
                if (ok && previousOk) AddRope(previous, ground);
                previous = ground;
                previousOk = ok;
            }
        }

        private bool IsFenceGround(Vector2 xz)
        {
            bool dry = _ground.Height(xz.x, xz.y, false) > _ground.WaterLevel + 0.02f;
            return dry && !_config.path.Contains(xz);
        }

        private void AddPost(Vector3 ground)
        {
            Vector3 size = _config.fencePostSize;
            _posts.Add(Matrix4x4.TRS(ground + Vector3.up * (size.y * 0.5f), Quaternion.identity, size));
        }

        private void AddRope(Vector3 a, Vector3 b)
        {
            float height = _config.fencePostSize.y * _config.fenceRopeHeight;
            Vector3 lift = Vector3.up * height;
            Vector3 along = b - a;
            float thickness = _config.fenceRopeThickness;
            _ropes.Add(Matrix4x4.TRS((a + b) * 0.5f + lift, Quaternion.LookRotation(along), new Vector3(thickness, thickness, along.magnitude)));
        }

        private ScatterRenderer CreateRenderer(string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            return go.AddComponent<ScatterRenderer>();
        }
    }
}
