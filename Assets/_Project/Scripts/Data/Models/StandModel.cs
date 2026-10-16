using System;

namespace BeachTycoon.Data
{
    /// <summary>Serializable state of one product stand. Orders in the queue are runtime-only.</summary>
    [Serializable]
    public sealed class StandModel
    {
        public string productId;
        public int lotIndex;

        /// <summary>Default place in the row of stands beside the main building (unlock order).</summary>
        public int slotIndex;

        /// <summary>
        /// False: the stand stays in its default slot beside the main building and follows it as it grows.
        /// True: it stands at x/z (moved by the player, or the building itself was moved).
        /// </summary>
        public bool placed;
        public float x;
        public float z;
        /// <summary>Turn around the vertical axis in degrees (v9; 0 = as built, facing the camera).</summary>
        public float yaw;

        /// <summary>Upgrade level, 0 = not upgraded.</summary>
        public int level;

        /// <summary>A manager was hired (v10): sale coins collect themselves.</summary>
        public bool manager;
    }
}
