using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Which build-bar tab a product is listed under (decorations have their own tab).</summary>
    public enum BuildCategory
    {
        Stands = 0,
        Food = 1,
        Services = 2,
    }

    /// <summary>What a service building does for the stands within its reach. None = it sells like a stand.</summary>
    public enum ServiceKind
    {
        None = 0,
        Weddings = 1,
        FirstAid = 2,
    }

    /// <summary>
    /// A service building (M20): it sells nothing; while someone works there it helps every stand within reach
    /// (better with their skill and its level). A chapel's guests pay more and its weddings pay out; a first-aid
    /// station keeps people waiting in line longer, and grows into a hospital.
    /// </summary>
    [Serializable]
    public sealed class ServiceEffect
    {
        public ServiceKind kind;
        [Tooltip("Metres from the building to a stand's centre that still count as in reach.")]
        [Min(0f)] public float reach = 8f;
        [Tooltip("Weddings: extra coins per sale at stands in reach. First aid: extra patience in line there.")]
        [Range(0f, 2f)] public float amount = 0.15f;
        [Tooltip("Weddings: seconds between weddings with one worker at full speed.")]
        [Min(1f)] public float eventSeconds = 240f;
        [Tooltip("Popularity added to the beach while someone works here.")]
        [Min(0f)] public float popularity;

        [Header("Growing (first aid into a hospital)")]
        [Tooltip("Main building tier from which it grows (0 = never). Its prefab's GrownLook shows from then on.")]
        [Min(0)] public int grownTier;
        public string grownName;
        [Tooltip("Reach and amount are multiplied by this once grown.")]
        [Min(1f)] public float grownScale = 1.5f;
    }
}
