using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Creates Greece, Spain and Italy the first time (tuning in the Inspector is kept) and lists every culture
    /// asset in the Cultures folder in the GameDatabase. A new culture is a new asset in that folder.
    /// </summary>
    internal static class CultureAssetBuilder
    {
        public const string Folder = ProjectPaths.Data + "/Cultures";
        private static readonly string[] Walls = { "plaster", "houseA", "houseB", "houseC" };
        private static readonly string[] Roofs = { "roof", "roofBlue", "roofGreen", "thatch" };

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(Folder);
            Create("Greece", 0, Greece, new[] { "#F7F7F2", "#F4F4EE", "#EEF2F6", "#F7F3EA" }, new[] { "#4A86D8", "#3C7ACF", "#5590DE", "#4A86D8" }, "#1F5AA6",
                "Agia Marina", "Kalamaki", "Paralia", "Kamari", "Perissa", "Platanias", "Elounda", "Thalassa", "Kokkini Bay", "Lefki Akti");
            Create("Spain", 1, Spain, new[] { "#E9C27C", "#E3B46A", "#EFD09A", "#DDA868" }, new[] { "#C4622D", "#B9572B", "#CF6E3A", "#C4622D" }, "#C0392B",
                "Playa Dorada", "Cala Blanca", "Costa Brava", "Puerto Sol", "Cala Azul", "Playa Serena", "Sol y Mar", "Roca Blanca", "Bahia Bonita", "Costa Luz");
            Create("Italy", 2, Italy, new[] { "#F1E6C8", "#EBDDB8", "#F3E3C3", "#E6D2AE" }, new[] { "#8E3B2A", "#9A4433", "#833525", "#8E3B2A" }, "#2E8B57",
                "Marina Piccola", "Porto Sole", "Riva Azzurra", "Spiaggia d'Oro", "Lido Bello", "Cala Luna", "Baia Verde", "Costa Serena", "Isola Bella", "Punta Rosa");
            database.cultures = LoadAll();
        }

        private static CultureDefinition[] LoadAll()
        {
            var cultures = new List<CultureDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:CultureDefinition", new[] { Folder }))
            {
                var culture = AssetDatabase.LoadAssetAtPath<CultureDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (culture != null && !string.IsNullOrEmpty(culture.id)) cultures.Add(culture);
            }

            cultures.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.id, b.id));
            return cultures.ToArray();
        }

        private static void Create(string name, int order, FlagDesign flag, string[] walls, string[] roofs, string tentStripe, params string[] villageNames)
        {
            var culture = DataAssetBuilder.LoadOrCreate<CultureDefinition>($"{Folder}/{name}.asset", out bool created);
            if (!created) return;

            culture.id = name.ToLowerInvariant();
            culture.displayName = name;
            culture.order = order;
            culture.flag = flag;
            var recolors = new List<PaletteRecolor>();
            for (int i = 0; i < Walls.Length; i++) recolors.Add(Recolor(Walls[i], walls[i]));
            for (int i = 0; i < Roofs.Length; i++) recolors.Add(Recolor(Roofs[i], roofs[i]));
            recolors.Add(Recolor("tentStripe", tentStripe));
            culture.recolors = recolors.ToArray();
            culture.villageNames = villageNames;
            EditorUtility.SetDirty(culture);
        }

        // Nine blue and white stripes with a white cross on a blue square in the corner.
        private static FlagDesign Greece => new FlagDesign
        {
            stripes = Colors("#0D5EAF", "#FFFFFF", "#0D5EAF", "#FFFFFF", "#0D5EAF", "#FFFFFF", "#0D5EAF", "#FFFFFF", "#0D5EAF"),
            canton = true,
            cantonSize = new Vector2(5f / 9f * 2f / 3f, 5f / 9f),
            cantonColor = Hex("#0D5EAF"),
            crossColor = Color.white,
            crossWidth = 0.2f,
        };

        // Red, yellow (twice as tall) and red.
        private static FlagDesign Spain => new FlagDesign
        {
            stripes = Colors("#AA151B", "#F1BF00", "#AA151B"),
            stripeWeights = new[] { 1f, 2f, 1f },
        };

        // Green, white and red, side by side.
        private static FlagDesign Italy => new FlagDesign
        {
            vertical = true,
            stripes = Colors("#009246", "#F4F5F0", "#CE2B37"),
        };

        private static PaletteRecolor Recolor(string key, string hex) => new PaletteRecolor { paletteKey = key, color = Hex(hex) };

        private static Color[] Colors(params string[] hex)
        {
            var colors = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++) colors[i] = Hex(hex[i]);
            return colors;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
