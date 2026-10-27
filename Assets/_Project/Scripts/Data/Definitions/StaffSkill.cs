namespace BeachTycoon.Data
{
    /// <summary>What a worker is good at, 1 to 5 stars each. Every stand uses one (ProductDefinition.skill).</summary>
    public enum StaffSkill
    {
        Cooking = 0,
        Selling = 1,
        Service = 2,
        Sport = 3,
        Care = 4,
    }

    public static class StaffSkills
    {
        public const int Count = 5;
        public const int MaxStars = 5;

        private static readonly string[] Names = { "Cooking", "Selling", "Service", "Sport", "Care" };

        public static string Name(StaffSkill skill) => Names[(int)skill];
    }
}
