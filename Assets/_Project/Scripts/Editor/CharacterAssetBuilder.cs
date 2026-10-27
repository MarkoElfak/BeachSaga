using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Creates Nikos (Greece), Lucía (Spain) and Marco (Italy) the first time (tuning in the Inspector is kept),
    /// gives each culture without a character its own, and rebuilds every character's prefab from its look.
    /// </summary>
    internal static class CharacterAssetBuilder
    {
        public const string Folder = ProjectPaths.Data + "/Characters";
        public const string PrefabFolder = ProjectPaths.Prefabs + "/Characters";
        private const string Ability = "Their stand serves 25% faster.";

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(Folder);
            ProjectPaths.EnsureFolder(PrefabFolder);
            Link(database, "greece", Create("Nikos", "Fisherman from Naxos", "shirtBlue", "shorts", "skinMid", "hairDark", false,
                CharacterHeadwear.Cap, "shorts", "#9FC9F0"));
            Link(database, "spain", Create("Lucía", "Flamenco dancer from Seville", "flagRed", "shorts", "skin", "hairDark", true,
                CharacterHeadwear.Flower, "towelYellow", "#F6D57A"));
            Link(database, "italy", Create("Marco", "Pizza chef from Naples", "shirtWhite", "shirtGreen", "skin", "hairDark", false,
                CharacterHeadwear.ChefHat, "shirtWhite", "#9ED6A8"));

            foreach (CultureDefinition culture in database.cultures)
            {
                if (culture.character != null) BuildPrefab(culture.character, database.palette);
            }
        }

        private static CharacterDefinition Create(string name, string tagline, string shirt, string shorts, string skin, string hair,
            bool longHair, CharacterHeadwear headwear, string headwearColor, string background)
        {
            string file = name == "Lucía" ? "Lucia" : name;
            var character = DataAssetBuilder.LoadOrCreate<CharacterDefinition>($"{Folder}/{file}.asset", out bool created);
            if (!created) return character;

            character.id = file.ToLowerInvariant();
            character.displayName = name;
            character.tagline = tagline;
            character.ability = Ability;
            character.shirt = shirt;
            character.shorts = shorts;
            character.skin = skin;
            character.hair = hair;
            character.longHair = longHair;
            character.headwear = headwear;
            character.headwearColor = headwearColor;
            ColorUtility.TryParseHtmlString(background, out character.portraitBackground);
            EditorUtility.SetDirty(character);
            return character;
        }

        // A culture keeps a character someone already gave it.
        private static void Link(GameDatabase database, string cultureId, CharacterDefinition character)
        {
            CultureDefinition culture = database.FindCulture(cultureId);
            if (culture == null || culture.character != null) return;
            culture.character = character;
            EditorUtility.SetDirty(culture);
        }

        private static void BuildPrefab(CharacterDefinition character, PaletteDefinition palette)
        {
            GameObject prefab = PrimitiveKit.SavePrefab(CharacterPrefabs.Build(character, palette), $"{PrefabFolder}/{character.id}.prefab");
            if (character.prefab == prefab) return;
            character.prefab = prefab;
            EditorUtility.SetDirty(character);
        }
    }
}
