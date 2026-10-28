using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>Pure staff maths: serving speed, levels and wages.</summary>
    public static class StaffMath
    {
        /// <summary>Serving speed with this worker at a stand that uses the skill (1 = full speed).</summary>
        public static float ServeSpeed(StaffConfig config, WorkerModel worker, StaffSkill skill, bool ownCountry)
        {
            float speed = 1f + config.bonusPerStar * (worker.Stars(skill) - 1) + (ownCountry ? config.ownCountryBonus : 0f);
            return worker.IsUnhappy ? speed * config.unhappySpeedFactor : speed;
        }

        /// <summary>
        /// Speed of a place with several people at work (a restaurant): the fastest one's speed plus a share of
        /// everyone else's. 'others' is the sum of the other people's speeds.
        /// </summary>
        public static float TeamSpeed(StaffConfig config, float fastest, float others) => fastest + others * config.extraWorkerShare;

        /// <summary>Sales needed at this level to reach the next one (0 at the top level).</summary>
        public static int SalesToNextLevel(StaffConfig config, int level) =>
            level >= config.maxLevel ? 0 : config.salesPerLevel * Math.Max(1, level);

        /// <summary>Coins per payday: a base, more for every star and every level above the first.</summary>
        public static long Wage(StaffConfig config, WorkerModel worker)
        {
            int stars = 0;
            for (int i = 0; i < StaffSkills.Count; i++) stars += worker.Stars((StaffSkill)i);
            return config.baseWage + config.wagePerStar * stars + config.wagePerLevel * (Math.Max(1, worker.level) - 1);
        }

        /// <summary>Adds one sale; returns true when it completes a level (xp starts over, the level goes up).</summary>
        public static bool AddSale(StaffConfig config, WorkerModel worker)
        {
            int needed = SalesToNextLevel(config, worker.level);
            if (needed <= 0) return false;
            worker.xp++;
            if (worker.xp < needed) return false;
            worker.xp = 0;
            worker.level++;
            return true;
        }
    }
}
