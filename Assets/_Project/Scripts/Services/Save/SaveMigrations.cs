using System.Collections.Generic;

namespace BeachTycoon.Services
{
    /// <summary>Every save migration step, oldest first. SaveService uses this list by default.</summary>
    public static class SaveMigrations
    {
        public static IEnumerable<ISaveMigration> All => new ISaveMigration[]
        {
            new AddNeighboursAndDecorations(),
            new AddMeta(),
            new AddPositions(),
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

        /// <summary>v2 -> v3: tutorial, settings, offline reward and monetization state (MetaModel defaults).</summary>
        private sealed class AddMeta : ISaveMigration
        {
            public int FromVersion => 2;
            public string Migrate(string json) => json.Replace("\"version\":2", "\"version\":3");
        }

        /// <summary>v3 -> v4: lots and decorations can be moved (placed/x/z default to "use the layout").</summary>
        private sealed class AddPositions : ISaveMigration
        {
            public int FromVersion => 3;
            public string Migrate(string json) => json.Replace("\"version\":3", "\"version\":4");
        }
    }
}
