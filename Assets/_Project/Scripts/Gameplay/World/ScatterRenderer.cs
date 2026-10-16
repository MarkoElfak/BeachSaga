using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Draws many copies of one mesh with GPU instancing (no GameObjects). Matrices only change when objects are
    /// moved (SetHidden), so the per-frame cost is one RenderMeshInstanced call per batch with no allocations.
    /// </summary>
    public sealed class ScatterRenderer : MonoBehaviour
    {
        private const int MaxInstancesPerBatch = 1023;

        private readonly List<Matrix4x4[]> _batches = new List<Matrix4x4[]>();
        private readonly List<Matrix4x4> _all = new List<Matrix4x4>();
        private readonly List<Matrix4x4> _visible = new List<Matrix4x4>();
        private Mesh _mesh;
        private RenderParams _params;
        private bool _ready;

        public void Setup(Mesh mesh, Material material, List<Matrix4x4> matrices, bool castShadows)
        {
            _mesh = mesh;
            material.enableInstancing = true;
            _params = new RenderParams(material)
            {
                shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = true,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 200f),
            };

            _all.Clear();
            _all.AddRange(matrices);
            Rebuild(_all);
            _ready = mesh != null && material != null;
        }

        /// <summary>Hides every instance standing inside one of the rects (grown by margin), e.g. under buildings.</summary>
        public void SetHidden(IReadOnlyList<Rect> rects, float margin)
        {
            _visible.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                Vector3 p = _all[i].GetColumn(3);
                if (!IsInside(rects, p, margin)) _visible.Add(_all[i]);
            }

            Rebuild(_visible);
        }

        public static bool IsInside(IReadOnlyList<Rect> rects, Vector3 point, float margin)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                Rect r = rects[i];
                if (point.x > r.xMin - margin && point.x < r.xMax + margin && point.z > r.yMin - margin && point.z < r.yMax + margin)
                {
                    return true;
                }
            }

            return false;
        }

        private void Rebuild(List<Matrix4x4> matrices)
        {
            _batches.Clear();
            for (int start = 0; start < matrices.Count; start += MaxInstancesPerBatch)
            {
                int count = Mathf.Min(MaxInstancesPerBatch, matrices.Count - start);
                _batches.Add(matrices.GetRange(start, count).ToArray());
            }
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            for (int i = 0; i < _batches.Count; i++)
            {
                Graphics.RenderMeshInstanced(_params, _mesh, 0, _batches[i]);
            }
        }
    }
}
