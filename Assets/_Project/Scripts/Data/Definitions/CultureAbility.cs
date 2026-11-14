using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>What a country's ability does.</summary>
    public enum CultureAbilityKind
    {
        None = 0,
        [Tooltip("Visitors relax on the beach longer.")]
        LongerStays,
        [Tooltip("Sales earn more at night and while a beach event runs.")]
        NightAndEvents,
        [Tooltip("Every sale earns more.")]
        HigherPrices,
    }

    /// <summary>A country's bonus for the whole beach, such as Greek Filoxenia (visitors stay 20% longer).</summary>
    [Serializable]
    public sealed class CultureAbility
    {
        public string name;
        public CultureAbilityKind kind;
        [Tooltip("How much stronger: 0.2 = 20% longer stays or 20% higher earnings.")]
        [Range(0f, 1f)] public float amount = 0.2f;
    }
}
