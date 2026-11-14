using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Staff (M18): the StaffConfig with each country's worker names, the skill every product's stand uses (both
    /// written once, when the config is first created) and the worker prefab, rebuilt on every Setup.
    /// </summary>
    internal static class StaffAssetBuilder
    {
        private const string ConfigPath = ProjectPaths.Config + "/StaffConfig.asset";
        private const string PrefabPath = ProjectPaths.Prefabs + "/Characters/Worker.prefab";

        public static void Fill(GameDatabase database)
        {
            var config = DataAssetBuilder.LoadOrCreate<StaffConfig>(ConfigPath, out bool created);
            if (created)
            {
                config.names = new[]
                {
                    Names("greece", "Eleni", "Giorgos", "Maria", "Yannis", "Sofia", "Kostas", "Dimitra", "Petros", "Katerina", "Nikolas"),
                    Names("spain", "Carmen", "Javier", "Isabel", "Pablo", "Lucas", "Elena", "Diego", "Rosa", "Sergio", "Ana"),
                    Names("italy", "Giulia", "Luca", "Francesca", "Matteo", "Chiara", "Paolo", "Sara", "Andrea", "Elisa", "Giovanni"),
                };
                SetSkills(database);
            }

            ProjectPaths.EnsureFolder(ProjectPaths.Prefabs + "/Characters");
            config.workerPrefab = PrimitiveKit.SavePrefab(WorkerPrefabs.Build(database.palette), PrefabPath);
            EditorUtility.SetDirty(config);
            database.staff = config;
        }

        private static WorkerNames Names(string cultureId, params string[] names) => new WorkerNames { cultureId = cultureId, names = names };

        // Donuts need a cook; towels and houses a seller; drinks good service; surf lessons a sporty teacher.
        private static void SetSkills(GameDatabase database)
        {
            Skill(database, "donuts", StaffSkill.Cooking);
            Skill(database, "towels", StaffSkill.Selling);
            Skill(database, "drinks", StaffSkill.Service);
            Skill(database, "surf_lessons", StaffSkill.Sport);
            Skill(database, "houses", StaffSkill.Selling);
        }

        private static void Skill(GameDatabase database, string productId, StaffSkill skill)
        {
            ProductDefinition product = database.FindProduct(productId);
            if (product == null) return;
            product.skill = skill;
            EditorUtility.SetDirty(product);
        }
    }
}
