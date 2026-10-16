using System;
using System.Text;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>JSON form of an IslandSnapshot, with the 50 KB size budget and a version check on read.</summary>
    public static class IslandSnapshotJson
    {
        public const int MaxBytes = 50 * 1024;

        public static string ToJson(IslandSnapshot snapshot) => JsonUtility.ToJson(snapshot);

        public static int ByteCount(string json) => string.IsNullOrEmpty(json) ? 0 : Encoding.UTF8.GetByteCount(json);

        public static bool FitsBudget(string json) => ByteCount(json) <= MaxBytes;

        /// <summary>False for empty, oversized, malformed or newer-than-supported data.</summary>
        public static bool TryParse(string json, out IslandSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrEmpty(json) || !FitsBudget(json)) return false;
            try
            {
                snapshot = JsonUtility.FromJson<IslandSnapshot>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (snapshot == null || snapshot.version < 1 || snapshot.version > IslandSnapshot.CurrentVersion)
            {
                snapshot = null;
                return false;
            }

            return true;
        }
    }
}
