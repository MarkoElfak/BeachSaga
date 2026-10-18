using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Creates the shared palette and one URP Lit material per entry (colour, smoothness, metallic and an optional
    /// procedural detail texture). Existing colours are kept; new entries and surface settings are added on upgrade.
    /// </summary>
    internal static class PaletteBuilder
    {
        private const string PalettePath = ProjectPaths.Data + "/Palette.asset";
        private const int CurrentDataVersion = 2;

        private enum Tex { None, Planks, Shingles, Thatch, Fabric, Plaster, Leaf }

        private readonly struct Entry
        {
            public readonly string Key, Hex;
            public readonly float Smoothness, Metallic;
            public readonly Tex Texture;

            public Entry(string key, string hex, float smoothness = 0.1f, Tex texture = Tex.None, float metallic = 0f)
            {
                Key = key; Hex = hex; Smoothness = smoothness; Texture = texture; Metallic = metallic;
            }
        }

        private static readonly Entry[] Defaults =
        {
            new Entry("sea", "#3F96D1", 0.9f), new Entry("shallows", "#7CC8E8", 0.9f), new Entry("foam", "#FFFFFF", 0.2f),
            new Entry("wetSand", "#C9A877", 0.5f), new Entry("drySand", "#EBD6A4", 0.08f), new Entry("grass", "#8DB86B", 0.05f),
            new Entry("palmTrunk", "#8A6A48", 0.1f, Tex.Thatch), new Entry("palmLeaf", "#4E9A4A", 0.25f, Tex.Leaf),
            new Entry("tentCanvas", "#F4F1E8", 0.1f, Tex.Fabric), new Entry("tentStripe", "#E8604C", 0.1f, Tex.Fabric),
            new Entry("wood", "#B07A4A", 0.15f, Tex.Planks), new Entry("awning", "#F28DB2", 0.1f, Tex.Fabric),
            new Entry("dough", "#D9A066", 0.3f), new Entry("icing", "#F27BB0", 0.6f), new Entry("coin", "#FFC93C", 0.75f, Tex.None, 0.9f),
            new Entry("lotMarker", "#D9C18C", 0.1f), new Entry("barBack", "#3A3A3A", 0.3f), new Entry("barFill", "#6BD16B", 0.4f),
            new Entry("roof", "#B5523B", 0.2f, Tex.Shingles), new Entry("window", "#9FD3E6", 0.9f), new Entry("skin", "#F2C6A0", 0.3f),
            new Entry("shorts", "#2F3E4E", 0.1f, Tex.Fabric), new Entry("shirtBlue", "#3A7BD5", 0.1f, Tex.Fabric),
            new Entry("shirtOrange", "#F28C28", 0.1f, Tex.Fabric), new Entry("shirtPurple", "#9B59B6", 0.1f, Tex.Fabric),
            new Entry("towelRed", "#E74C3C", 0.05f, Tex.Fabric), new Entry("towelYellow", "#F1C40F", 0.05f, Tex.Fabric),
            new Entry("towelTeal", "#1ABC9C", 0.05f, Tex.Fabric),
            // Milestone 2
            new Entry("thatch", "#C9A86A", 0.05f, Tex.Thatch), new Entry("plaster", "#F2EEE6", 0.15f, Tex.Plaster),
            new Entry("glass", "#7FB8D9", 0.92f), new Entry("pool", "#4FC3E0", 0.95f), new Entry("metal", "#9AA3AB", 0.6f, Tex.None, 0.7f),
            new Entry("flagRed", "#D9483B", 0.1f, Tex.Fabric), new Entry("bottle", "#3E8E5A", 0.85f),
            new Entry("surfYellow", "#F2C230", 0.7f), new Entry("surfRed", "#E2533C", 0.7f), new Entry("surfBlue", "#3A8FD6", 0.7f),
            new Entry("houseA", "#E8D4B0", 0.12f, Tex.Plaster), new Entry("houseB", "#BFD8C8", 0.12f, Tex.Plaster),
            new Entry("houseC", "#F0C8B0", 0.12f, Tex.Plaster), new Entry("roofBlue", "#4A6FA5", 0.2f, Tex.Shingles),
            new Entry("roofGreen", "#5E8C5A", 0.2f, Tex.Shingles), new Entry("cushion", "#5DADE2", 0.1f, Tex.Fabric),
            new Entry("umbrellaRed", "#E74C3C", 0.15f, Tex.Fabric), new Entry("umbrellaWhite", "#F7F4EC", 0.15f, Tex.Fabric),
            new Entry("castle", "#E3C78C", 0.05f), new Entry("hairDark", "#4A3426", 0.3f), new Entry("hairBlond", "#D9B46C", 0.3f),
            new Entry("skinDark", "#8F5F42", 0.3f), new Entry("skinMid", "#C98E66", 0.3f), new Entry("shirtGreen", "#27AE60", 0.1f, Tex.Fabric),
            new Entry("shirtWhite", "#F2F2F2", 0.1f, Tex.Fabric), new Entry("shirtPink", "#E88AB4", 0.1f, Tex.Fabric),
            new Entry("hedge", "#4F8A3F", 0.05f, Tex.Leaf), new Entry("stone", "#B9B2A6", 0.2f, Tex.Plaster),
            new Entry("placeValid", "#4CD964", 0.3f), new Entry("placeInvalid", "#FF3B30", 0.3f),
            new Entry("placeValidEdge", "#1B7A35", 0.3f), new Entry("placeInvalidEdge", "#A3170F", 0.3f),
            new Entry("fencePost", "#8A6A47", 0.1f), new Entry("fenceRope", "#E8D9B5", 0.1f),
            // Milestone 14: the culture flag cloth (white, so the flag image shows its true colours)
            new Entry("flag", "#FFFFFF", 0.1f),
        };

        public static PaletteDefinition BuildOrSync()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.Materials);
            ProjectPaths.EnsureFolder(ProjectPaths.Data);

            var palette = AssetDatabase.LoadAssetAtPath<PaletteDefinition>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<PaletteDefinition>();
                palette.entries = new PaletteEntry[0];
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            Upgrade(palette);
            Shader shader = Shader.Find(RenderingAssets.LitShader);
            for (int i = 0; i < palette.entries.Length; i++)
            {
                palette.entries[i].material = SyncMaterial(palette.entries[i], shader);
            }

            EditorUtility.SetDirty(palette);
            return palette;
        }

        // Adds missing entries; on first upgrade also applies the default surface settings to existing ones.
        private static void Upgrade(PaletteDefinition palette)
        {
            var entries = new List<PaletteEntry>(palette.entries ?? new PaletteEntry[0]);
            bool applySurfaces = palette.dataVersion < CurrentDataVersion;
            foreach (Entry entry in Defaults)
            {
                int index = entries.FindIndex(e => e.key == entry.Key);
                if (index < 0)
                {
                    ColorUtility.TryParseHtmlString(entry.Hex, out Color color);
                    entries.Add(Surface(new PaletteEntry { key = entry.Key, color = color }, entry));
                }
                else if (applySurfaces)
                {
                    entries[index] = Surface(entries[index], entry);
                }
            }

            palette.entries = entries.ToArray();
            palette.dataVersion = CurrentDataVersion;
        }

        private static PaletteEntry Surface(PaletteEntry target, Entry source)
        {
            target.smoothness = source.Smoothness;
            target.metallic = source.Metallic;
            target.texture = LoadTexture(source.Texture);
            target.tiling = Vector2.one;
            return target;
        }

        private static Texture2D LoadTexture(Tex texture)
        {
            switch (texture)
            {
                case Tex.Planks: return TextureFactory.Planks;
                case Tex.Shingles: return TextureFactory.Shingles;
                case Tex.Thatch: return TextureFactory.Thatch;
                case Tex.Fabric: return TextureFactory.Fabric;
                case Tex.Plaster: return TextureFactory.Plaster;
                case Tex.Leaf: return TextureFactory.Leaf;
                default: return null;
            }
        }

        private static Material SyncMaterial(PaletteEntry entry, Shader shader)
        {
            string path = $"{ProjectPaths.Materials}/M_{entry.key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", entry.color);
            material.SetTexture("_BaseMap", entry.texture);
            Vector2 tiling = entry.tiling == Vector2.zero ? Vector2.one : entry.tiling;
            material.SetTextureScale("_BaseMap", tiling);
            material.SetFloat("_Smoothness", entry.smoothness);
            material.SetFloat("_Metallic", entry.metallic);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
