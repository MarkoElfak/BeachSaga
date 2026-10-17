using System;
using System.Collections.Generic;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Everything another player needs to see an island: who owns it, the main tier, every building, stand and
    /// decoration with its position, popularity and when it was taken. Serializes to JSON (well under 50 KB).
    /// Bump CurrentVersion when the layout changes.
    /// </summary>
    [Serializable]
    public sealed class IslandSnapshot
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string ownerId = string.Empty;
        public string ownerName = string.Empty;

        /// <summary>Name of the owner's village (empty on snapshots from before villages existed).</summary>
        public string villageName = string.Empty;

        /// <summary>Main building tier (0 before the tent is built).</summary>
        public int tier;
        public float popularity;

        /// <summary>UTC time the snapshot was taken, ISO 8601 round-trip format.</summary>
        public string timestampUtc = string.Empty;

        public List<SnapshotBuilding> buildings = new List<SnapshotBuilding>();
        public List<SnapshotStand> stands = new List<SnapshotStand>();
        public List<SnapshotDecoration> decorations = new List<SnapshotDecoration>();
    }

    /// <summary>A building on a lot: lot 0 is the main building, the others are neighbour houses.</summary>
    [Serializable]
    public sealed class SnapshotBuilding
    {
        public int lotIndex;
        public int tier;
        public int variant;
        public float x;
        public float z;
        public float yaw;
    }

    [Serializable]
    public sealed class SnapshotStand
    {
        public string productId = string.Empty;
        public int level;
        public float x;
        public float z;
        public float yaw;
        public bool manager;
    }

    [Serializable]
    public sealed class SnapshotDecoration
    {
        public string decorationId = string.Empty;
        public float x;
        public float z;
        public float yaw;
    }
}
