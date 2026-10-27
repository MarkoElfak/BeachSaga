using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>First names a culture's workers can have.</summary>
    [Serializable]
    public sealed class WorkerNames
    {
        public string cultureId;
        public string[] names = new string[0];
    }

    /// <summary>
    /// Hired staff (M18): how many the main building allows, how job seekers arrive, how workers speed up a stand and
    /// grow, and what they are paid. Wages are tuned with the rest of the economy later (M21).
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Staff", fileName = "StaffConfig")]
    public sealed class StaffConfig : ScriptableObject
    {
        [Header("Slots and job seekers")]
        [Tooltip("Staff allowed per main building tier (element 0 is the Tent).")]
        public int[] slotsPerTier = { 0, 2, 4, 7, 10 };
        [Tooltip("Job seekers waiting on the beach at most (only while a staff slot is free).")]
        [Min(0)] public int maxJobSeekers = 3;
        [Tooltip("Seconds between new job seekers arriving; an upgrade brings a full set at once.")]
        [Min(1f)] public float seekerArrivalSeconds = 90f;
        [Tooltip("Stars a new job seeker has in each skill (min, max inclusive).")]
        public Vector2Int newSkillStars = new Vector2Int(1, 3);

        [Header("Work")]
        [Tooltip("Serving speed of a stand nobody works at (1 = full speed).")]
        [Range(0.1f, 1f)] public float unstaffedServeSpeed = 0.5f;
        [Tooltip("Extra serving speed per star above one in the stand's skill.")]
        [Range(0f, 0.5f)] public float bonusPerStar = 0.04f;
        [Tooltip("Extra serving speed for a worker from the player's own country.")]
        [Range(0f, 0.5f)] public float ownCountryBonus = 0.05f;
        [Tooltip("Serving speed is multiplied by this while a worker's wage is unpaid.")]
        [Range(0.1f, 1f)] public float unhappySpeedFactor = 0.75f;
        [Tooltip("Sales to reach the next level = this x the current level.")]
        [Min(1)] public int salesPerLevel = 30;
        [Min(1)] public int maxLevel = 5;
        [Tooltip("From this level a worker also collects their stand's coins (what managers did before).")]
        [Min(1)] public int collectCoinsLevel = 3;

        [Header("Wages (coins per payday)")]
        [Min(1f)] public float paySeconds = 60f;
        [Min(0)] public long baseWage = 10;
        [Min(0)] public long wagePerStar = 2;
        [Min(0)] public long wagePerLevel = 10;
        [Tooltip("Missed paydays before the worker warns they will quit.")]
        [Min(1)] public int warnAfterMissedPays = 2;
        [Tooltip("Missed paydays in a row before the worker quits (only while playing).")]
        [Min(1)] public int quitAfterMissedPays = 3;

        [Header("Looks (palette keys) and names")]
        public string[] shirts = { "shirtBlue", "shirtOrange", "shirtPurple", "shirtGreen", "shirtPink", "towelTeal", "towelYellow" };
        public string[] skins = { "skin", "skinMid", "skinDark" };
        public string[] hairs = { "hairDark", "hairBlond", "wood" };
        public WorkerNames[] names = new WorkerNames[0];

        [Tooltip("Built by Setup: a WorkerView person recoloured per worker.")]
        public GameObject workerPrefab;

        /// <summary>Staff allowed with the main building at this tier (0 = no building).</summary>
        public int SlotsFor(int tier)
        {
            if (tier <= 0 || slotsPerTier == null || slotsPerTier.Length == 0) return 0;
            return slotsPerTier[Mathf.Min(tier, slotsPerTier.Length) - 1];
        }
    }
}
