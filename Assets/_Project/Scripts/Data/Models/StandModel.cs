using System;

namespace BeachTycoon.Data
{
    /// <summary>Serializable state of one product stand. Orders in the queue are runtime-only.</summary>
    [Serializable]
    public sealed class StandModel
    {
        public string productId;
        public int lotIndex;

        /// <summary>Which anchor on the building the stand sits at.</summary>
        public int slotIndex;

        /// <summary>Upgrade level, 0 = not upgraded.</summary>
        public int level;
    }
}
