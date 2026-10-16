using System;
using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Migration hook: runs registered ISaveMigration steps one version at a time until the JSON
    /// matches GameSaveData.CurrentVersion. Register new steps here when the save layout changes.
    /// </summary>
    public sealed class SaveMigrator
    {
        private readonly Dictionary<int, ISaveMigration> _steps = new Dictionary<int, ISaveMigration>();

        public SaveMigrator(IEnumerable<ISaveMigration> migrations = null)
        {
            if (migrations == null)
            {
                return;
            }

            foreach (ISaveMigration migration in migrations)
            {
                _steps[migration.FromVersion] = migration;
            }
        }

        public string MigrateToCurrent(string json, int fromVersion)
        {
            if (fromVersion > GameSaveData.CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"Save version {fromVersion} is newer than this build ({GameSaveData.CurrentVersion}).");
            }

            for (int version = fromVersion; version < GameSaveData.CurrentVersion; version++)
            {
                if (!_steps.TryGetValue(version, out ISaveMigration step))
                {
                    throw new InvalidOperationException($"No save migration from version {version}.");
                }

                json = step.Migrate(json);
            }

            return json;
        }
    }
}
