using System.Collections.Generic;

namespace BeachTycoon.Services
{
    /// <summary>Every save migration step, oldest first. SaveService uses this list by default.</summary>
    public static class SaveMigrations
    {
        public static IEnumerable<ISaveMigration> All => new ISaveMigration[]
        {
            new AddNeighboursAndDecorations(),
        };

        /// <summary>
        /// v1 -> v2: neighbours and decorations were added. JsonUtility fills missing lists with their
        /// defaults, so only the version number changes.
        /// </summary>
        private sealed class AddNeighboursAndDecorations : ISaveMigration
        {
            public int FromVersion => 1;

            public string Migrate(string json) => json.Replace("\"version\":1", "\"version\":2");
        }
    }
}
