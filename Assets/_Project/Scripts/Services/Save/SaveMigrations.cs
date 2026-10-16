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
            new AddStandPositions(),
            new AddDailyRewards(),
            new AddOnline(),
            new AddAchievements(),
            new AddRotation(),
            new AddManagers(),
            new AddSeasons(),
            new AddMiniGames(),
            new AddVolumes(),
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

        /// <summary>
        /// v4 -> v5: stands are placed on their own (placed/x/z). Old stands load unplaced, so they keep their
        /// default slot beside the main building.
        /// </summary>
        private sealed class AddStandPositions : ISaveMigration
        {
            public int FromVersion => 4;
            public string Migrate(string json) => json.Replace("\"version\":4", "\"version\":5");
        }

        /// <summary>v5 -> v6: daily login streak in MetaModel (defaults: never claimed).</summary>
        private sealed class AddDailyRewards : ISaveMigration
        {
            public int FromVersion => 5;
            public string Migrate(string json) => json.Replace("\"version\":5", "\"version\":6");
        }

        /// <summary>v6 -> v7: player id and visit-bonus bookkeeping in MetaModel (an id is created on load).</summary>
        private sealed class AddOnline : ISaveMigration
        {
            public int FromVersion => 6;
            public string Migrate(string json) => json.Replace("\"version\":6", "\"version\":7");
        }

        /// <summary>v7 -> v8: achievement progress (an empty list; level goals catch up on load).</summary>
        private sealed class AddAchievements : ISaveMigration
        {
            public int FromVersion => 7;
            public string Migrate(string json) => json.Replace("\"version\":7", "\"version\":8");
        }

        /// <summary>v8 -> v9: lots, stands and decorations can be turned (yaw defaults to 0, as before).</summary>
        private sealed class AddRotation : ISaveMigration
        {
            public int FromVersion => 8;
            public string Migrate(string json) => json.Replace("\"version\":8", "\"version\":9");
        }

        /// <summary>v9 -> v10: stands can have a manager (defaults to none, as before).</summary>
        private sealed class AddManagers : ISaveMigration
        {
            public int FromVersion => 9;
            public string Migrate(string json) => json.Replace("\"version\":9", "\"version\":10");
        }

        /// <summary>v10 -> v11: seasons. New fields start at zero (no seashells, first season, nothing earned yet).</summary>
        private sealed class AddSeasons : ISaveMigration
        {
            public int FromVersion => 10;
            public string Migrate(string json) => json.Replace("\"version\":10", "\"version\":11");
        }

        /// <summary>v11 -> v12: mini-games. The new list starts empty (no levels passed).</summary>
        private sealed class AddMiniGames : ISaveMigration
        {
            public int FromVersion => 11;
            public string Migrate(string json) => json.Replace("\"version\":11", "\"version\":12");
        }

        /// <summary>
        /// v12 -> v13: sound and music volumes replace the on/off switches. A switch that was off becomes volume 0;
        /// everything else gets the defaults (full volume, vibration on).
        /// </summary>
        private sealed class AddVolumes : ISaveMigration
        {
            public int FromVersion => 12;

            public string Migrate(string json) => json.Replace("\"version\":12", "\"version\":13")
                .Replace("\"soundOn\":false", "\"soundOn\":false,\"soundVolume\":0.0")
                .Replace("\"musicOn\":false", "\"musicOn\":false,\"musicVolume\":0.0");
        }
    }
}
