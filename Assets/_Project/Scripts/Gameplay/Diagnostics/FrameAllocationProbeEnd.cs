using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Closes FrameAllocationProbe's measurement after every other script's LateUpdate.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class FrameAllocationProbeEnd : MonoBehaviour
    {
        private FrameAllocationProbe _probe;

        public void Bind(FrameAllocationProbe probe) => _probe = probe;

        private void LateUpdate()
        {
            if (_probe != null) _probe.EndFrame();
        }
    }
}
