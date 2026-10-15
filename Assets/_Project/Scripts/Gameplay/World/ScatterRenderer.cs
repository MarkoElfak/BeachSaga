using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Draws many copies of one mesh with GPU instancing (no GameObjects). Matrices are fixed after Setup,
    /// so the per-frame cost is one RenderMeshInstanced call with no allocations.
    /// </summary>
    public sealed class ScatterRenderer : MonoBehaviour
    {
        private const int MaxInstancesPerBatch = 1023;

        private readonly List<Matrix4x4[]> _batches = new List<Matrix4x4[]>();
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

            _batches.Clear();
            for (int start = 0; start < matrices.Count; start += MaxInstancesPerBatch)
            {
                int count = Mathf.Min(MaxInstancesPerBatch, matrices.Count - start);
                _batches.Add(matrices.GetRange(start, count).ToArray());
            }

            _ready = mesh != null && material != null && matrices.Count > 0;
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
