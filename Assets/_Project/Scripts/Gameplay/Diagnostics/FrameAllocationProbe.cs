using System;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Measures managed allocations made by game scripts each frame: starts counting before every other
    /// Update and stops after every LateUpdate (FrameAllocationProbeEnd). Used by the performance playtest.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class FrameAllocationProbe : MonoBehaviour
    {
        private long _frameStart;
        private bool _counting;

        public int Frames { get; private set; }
        public int FramesWithAllocations { get; private set; }
        public long TotalBytes { get; private set; }
        public long MaxFrameBytes { get; private set; }
        public double TotalFrameSeconds { get; private set; }
        public float MaxFrameSeconds { get; private set; }

        public static FrameAllocationProbe Create()
        {
            var host = new GameObject("FrameAllocationProbe");
            var probe = host.AddComponent<FrameAllocationProbe>();
            host.AddComponent<FrameAllocationProbeEnd>().Bind(probe);
            return probe;
        }

        public void ResetStats()
        {
            Frames = 0;
            FramesWithAllocations = 0;
            TotalBytes = 0;
            MaxFrameBytes = 0;
            TotalFrameSeconds = 0;
            MaxFrameSeconds = 0f;
        }

        private void Update()
        {
            _counting = true;
            _frameStart = GC.GetAllocatedBytesForCurrentThread();
        }

        internal void EndFrame()
        {
            if (!_counting) return;
            _counting = false;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _frameStart;
            Frames++;
            TotalBytes += bytes;
            if (bytes > 0) FramesWithAllocations++;
            if (bytes > MaxFrameBytes) MaxFrameBytes = bytes;
            float frame = Time.unscaledDeltaTime;
            TotalFrameSeconds += frame;
            if (frame > MaxFrameSeconds) MaxFrameSeconds = frame;
        }
    }
}
