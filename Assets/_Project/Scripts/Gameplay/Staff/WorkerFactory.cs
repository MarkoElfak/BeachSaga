using System.Collections.Generic;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Makes job seekers: a country (any culture), a first name from it that nobody on the beach has yet, random looks
    /// from the staff palette keys and a few stars in every skill.
    /// </summary>
    public sealed class WorkerFactory
    {
        private readonly StaffConfig _config;
        private readonly IReadOnlyList<CultureDefinition> _cultures;
        private readonly System.Random _random;

        public WorkerFactory(StaffConfig config, IReadOnlyList<CultureDefinition> cultures, System.Random random)
        {
            _config = config;
            _cultures = cultures;
            _random = random;
        }

        public WorkerModel Create(int id, IReadOnlyList<StaffMember> onBeach, IReadOnlyList<StaffMember> alsoOnBeach)
        {
            var model = new WorkerModel { id = id };
            Fill(model, onBeach, alsoOnBeach);
            return model;
        }

        /// <summary>Gives a worker a country, a name, looks and stars in every skill not set yet (a manager from an older save).</summary>
        public void Fill(WorkerModel model, IReadOnlyList<StaffMember> onBeach, IReadOnlyList<StaffMember> alsoOnBeach)
        {
            if (_cultures.Count > 0) model.cultureId = _cultures[_random.Next(_cultures.Count)].id;
            model.name = PickName(model.cultureId, model.id, onBeach, alsoOnBeach);
            model.shirt = Pick(_config.shirts, "shirtBlue");
            model.skin = Pick(_config.skins, "skin");
            model.hair = Pick(_config.hairs, "hairDark");
            model.longHair = _random.Next(5) < 2;
            if (model.skills == null || model.skills.Length != StaffSkills.Count) model.skills = new int[StaffSkills.Count];
            for (int i = 0; i < StaffSkills.Count; i++)
            {
                if (model.skills[i] <= 0) model.skills[i] = _random.Next(_config.newSkillStars.x, _config.newSkillStars.y + 1);
            }
        }

        private string Pick(string[] keys, string fallback) => keys != null && keys.Length > 0 ? keys[_random.Next(keys.Length)] : fallback;

        // A random name from the country's list; the next one along when someone on the beach already has it.
        private string PickName(string cultureId, int id, IReadOnlyList<StaffMember> a, IReadOnlyList<StaffMember> b)
        {
            string[] names = NamesFor(cultureId);
            if (names.Length == 0) return "Worker " + id;
            int start = _random.Next(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[(start + i) % names.Length];
                if (!Taken(name, a) && !Taken(name, b)) return name;
            }

            return names[start];
        }

        private string[] NamesFor(string cultureId)
        {
            if (_config.names != null)
            {
                foreach (WorkerNames entry in _config.names)
                {
                    if (entry != null && entry.cultureId == cultureId && entry.names != null) return entry.names;
                }
            }

            return System.Array.Empty<string>();
        }

        private static bool Taken(string name, IReadOnlyList<StaffMember> members)
        {
            for (int i = 0; i < members.Count; i++) if (members[i].Model.name == name) return true;
            return false;
        }
    }
}
