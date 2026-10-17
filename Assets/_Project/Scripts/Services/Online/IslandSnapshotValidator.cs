using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Snapshots come from other players, so nothing in them is trusted: this returns a clean copy with known
    /// content only (tiers clamped, unknown products and decorations dropped, bad or duplicate entries removed,
    /// counts capped), or null when the snapshot is unusable.
    /// </summary>
    public static class IslandSnapshotValidator
    {
        public const int MaxDecorations = 250;
        public const int MaxNameLength = 24;
        private const float MaxCoordinate = 500f;
        private const float MaxPopularity = 100f;
        private const int MaxStandLevel = 999;

        public static IslandSnapshot Sanitize(IslandSnapshot raw, GameDatabase database)
        {
            if (raw == null || string.IsNullOrEmpty(raw.ownerId) || raw.version < 1 || raw.version > IslandSnapshot.CurrentVersion)
            {
                return null;
            }

            var clean = new IslandSnapshot
            {
                ownerId = raw.ownerId,
                ownerName = Truncate(raw.ownerName),
                villageName = Truncate(raw.villageName),
                popularity = IsFinite(raw.popularity) ? Mathf.Clamp(raw.popularity, 0f, MaxPopularity) : 0f,
                timestampUtc = raw.timestampUtc ?? string.Empty,
            };

            int lotCount = database.world.lotPositions != null ? database.world.lotPositions.Length : 1;
            var lots = new HashSet<int>();
            foreach (SnapshotBuilding building in raw.buildings ?? new List<SnapshotBuilding>())
            {
                if (building == null || building.lotIndex < 0 || building.lotIndex >= lotCount) continue;
                if (!IsFinite(building.x, building.z) || !lots.Add(building.lotIndex)) continue;
                bool main = building.lotIndex == 0;
                int tier = main ? Mathf.Clamp(building.tier, 0, database.MaxTier) : 1;
                clean.buildings.Add(new SnapshotBuilding
                {
                    lotIndex = building.lotIndex, tier = tier, variant = Mathf.Max(0, building.variant), x = building.x, z = building.z, yaw = Yaw(building.yaw),
                });
                if (main) clean.tier = tier;
            }

            var products = new HashSet<string>();
            foreach (SnapshotStand stand in raw.stands ?? new List<SnapshotStand>())
            {
                if (stand == null || database.FindProduct(stand.productId) == null || !IsFinite(stand.x, stand.z)) continue;
                if (!products.Add(stand.productId)) continue;
                clean.stands.Add(new SnapshotStand
                {
                    productId = stand.productId, level = Mathf.Clamp(stand.level, 0, MaxStandLevel), x = stand.x, z = stand.z, yaw = Yaw(stand.yaw),
                    manager = stand.manager,
                });
            }

            foreach (SnapshotDecoration decoration in raw.decorations ?? new List<SnapshotDecoration>())
            {
                if (clean.decorations.Count >= MaxDecorations) break;
                if (decoration == null || database.FindDecoration(decoration.decorationId) == null) continue;
                if (!IsFinite(decoration.x, decoration.z)) continue;
                clean.decorations.Add(new SnapshotDecoration { decorationId = decoration.decorationId, x = decoration.x, z = decoration.z, yaw = Yaw(decoration.yaw) });
            }

            return clean;
        }

        private static string Truncate(string name)
        {
            // No angle brackets: names are shown in rich text labels.
            string trimmed = (name ?? string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            return trimmed.Length > MaxNameLength ? trimmed.Substring(0, MaxNameLength) : trimmed;
        }

        private static float Yaw(float yaw) => IsFinite(yaw) ? Mathf.Repeat(yaw, 360f) : 0f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(float x, float z) =>
            IsFinite(x) && IsFinite(z) && Mathf.Abs(x) <= MaxCoordinate && Mathf.Abs(z) <= MaxCoordinate;
    }
}
