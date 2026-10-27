using System;

namespace BeachTycoon.Data
{
    /// <summary>
    /// One person on the payroll (GameSaveData.staff) or a job seeker waiting on the beach (jobSeekers). Saved since
    /// version 18. Looks are palette keys; an empty name is filled in on load (a manager turned into a worker).
    /// </summary>
    [Serializable]
    public sealed class WorkerModel
    {
        /// <summary>Unique among staff and job seekers, never reused (MetaModel.nextWorkerId).</summary>
        public int id;
        public string name = string.Empty;
        public string cultureId = string.Empty;

        /// <summary>Stars per StaffSkill, 1 to 5.</summary>
        public int[] skills = new int[StaffSkills.Count];

        /// <summary>1 to StaffConfig.maxLevel; each level adds a star to the skill of the stand they work at.</summary>
        public int level = 1;

        /// <summary>Sales served since the last level.</summary>
        public int xp;

        /// <summary>Stand the worker is assigned to; -1 while waiting for one (and for job seekers).</summary>
        public int stand = -1;

        /// <summary>Paydays missed in a row; any wage paid resets it.</summary>
        public int missedPays;

        public string shirt = string.Empty;
        public string skin = string.Empty;
        public string hair = string.Empty;
        public bool longHair;

        public int Stars(StaffSkill skill)
        {
            int index = (int)skill;
            return skills != null && index < skills.Length ? Math.Max(1, skills[index]) : 1;
        }

        public bool IsUnhappy => missedPays > 0;
    }
}
