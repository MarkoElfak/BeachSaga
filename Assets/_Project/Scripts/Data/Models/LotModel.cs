using System;

namespace BeachTycoon.Data
{
    public enum LotState
    {
        Empty = 0,
        Building = 1,
        Built = 2,
    }

    /// <summary>Serializable state of one buildable lot.</summary>
    [Serializable]
    public sealed class LotModel
    {
        public int index;
        public LotState state;

        /// <summary>Current building tier, 0 when nothing has been built yet.</summary>
        public int tier;

        /// <summary>Tier being constructed while state is Building.</summary>
        public int targetTier;

        /// <summary>House lots: which house prefab variant was built.</summary>
        public int variant;

        public float buildTotalSeconds;
        public float buildRemainingSeconds;

        public LotModel()
        {
        }

        public LotModel(int index)
        {
            this.index = index;
        }

        /// <summary>0..1 construction progress (1 when not building).</summary>
        public float BuildProgress =>
            buildTotalSeconds > 0f ? 1f - Math.Max(0f, buildRemainingSeconds) / buildTotalSeconds : 1f;
    }
}
