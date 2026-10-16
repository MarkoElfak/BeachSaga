using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Static batching for scenery spawned at runtime (it cannot be marked static in the scene).</summary>
    public static class StaticScenery
    {
        /// <summary>
        /// Combines every mesh under 'root' into static batches. Combined objects can still be hidden but
        /// never moved. Skipped (returns false) when a mesh does not allow CPU access, as runtime batching needs it.
        /// </summary>
        public static bool TryCombine(GameObject root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return false;
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) return false;
            }

            StaticBatchingUtility.Combine(root);
            return true;
        }
    }
}
