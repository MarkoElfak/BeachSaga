using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Crowds (dozens of visitors built from the same few meshes and palette materials) draw far fewer calls
    /// as GPU instances than through the SRP Batcher, which issues one draw per renderer. Giving a renderer a
    /// property block opts it out of the SRP Batcher, so URP instances it (palette materials enable instancing).
    /// </summary>
    public static class GpuInstancing
    {
        private static readonly int KeyId = Shader.PropertyToID("_InstancingKey");
        private static MaterialPropertyBlock _block;

        /// <summary>Call once per pooled instance when it is created (not per frame).</summary>
        public static void Enable(GameObject root)
        {
            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
                _block.SetFloat(KeyId, 0f); // any value: a non-empty block is what matters
            }

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(_block);
        }
    }
}
