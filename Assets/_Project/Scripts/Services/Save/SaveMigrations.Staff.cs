using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    public static partial class SaveMigrations
    {
        private const int ManagerLevel = 3;

        /// <summary>
        /// v17 -> v18: staff (M18). Managers merged into workers, so each hired manager becomes a level 3 worker at
        /// their stand (who keeps collecting its coins). Their name, country and looks are picked on load.
        /// </summary>
        private sealed class AddStaff : ISaveMigration
        {
            public int FromVersion => 17;

            public string Migrate(string json)
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                for (int i = 0; i < data.stands.Count; i++)
                {
                    if (!data.stands[i].manager) continue;
                    data.staff.Add(new WorkerModel { id = data.meta.nextWorkerId++, level = ManagerLevel, stand = i });
                }

                data.version = 18;
                return JsonUtility.ToJson(data);
            }
        }
    }
}
