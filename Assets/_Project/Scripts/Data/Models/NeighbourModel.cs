using System;

namespace BeachTycoon.Data
{
    /// <summary>A resident living in a house lot. Their current quest progress is saved with them.</summary>
    [Serializable]
    public sealed class NeighbourModel
    {
        public string name;
        public int lotIndex;
        public float rentTimer;
        public string questId = string.Empty;
        public long questProgress;
        public int questsCompleted;
    }

    /// <summary>A placed decoration.</summary>
    [Serializable]
    public sealed class DecorationModel
    {
        public string decorationId;
        public int spotIndex;
    }
}
