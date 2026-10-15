using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Generates tileable greyscale detail textures (wood, shingles, thatch, fabric, plaster, leaf, noise) as PNGs.
    /// They are mostly light so the palette colour tints them. No external art involved.
    /// </summary>
    internal static class TextureFactory
    {
        public const string Folder = ProjectPaths.Root + "/Art/Textures";
        private const int Size = 256;

        public static Texture2D Planks => Get("T_Planks", PlanksPixel);
        public static Texture2D Shingles => Get("T_Shingles", ShinglesPixel);
        public static Texture2D Thatch => Get("T_Thatch", ThatchPixel);
        public static Texture2D Fabric => Get("T_Fabric", FabricPixel);
        public static Texture2D Plaster => Get("T_Plaster", PlasterPixel);
        public static Texture2D Leaf => Get("T_Leaf", LeafPixel);
        public static Texture2D Noise => Get("T_Noise", (x, y) => Fbm(x, y, 8, 4), linear: true);
        public static Texture2D FlatNormal => Get("T_FlatNormal", (x, y) => 1f, normalMap: true);

        private delegate float Pixel(float x, float y);

        private static Texture2D Get(string name, Pixel pixel, bool linear = false, bool normalMap = false)
        {
            string path = $"{Folder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            ProjectPaths.EnsureFolder(Folder);
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var colors = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    byte v = (byte)(Mathf.Clamp01(pixel((float)x / Size, (float)y / Size)) * 255f);
                    colors[y * Size + x] = normalMap ? new Color32(128, 128, 255, 255) : new Color32(v, v, v, 255);
                }
            }

            texture.SetPixels32(colors);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Horizontal boards with grain, per-board tone and dark gaps.
        private static float PlanksPixel(float x, float y)
        {
            const int boards = 6;
            float row = y * boards;
            int board = Mathf.FloorToInt(row);
            float inBoard = row - board;
            float seam = Mathf.Repeat(x + Hash(board) * 0.7f, 1f);
            float grain = Fbm(x + Hash(board), y * 6f, 4, 3); // long streaks along the board, still tileable
            float tone = 0.8f + Hash(board + 17) * 0.15f;
            float gap = Mathf.Min(Mathf.SmoothStep(0f, 0.08f, inBoard), Mathf.SmoothStep(0f, 0.08f, 1f - inBoard));
            float seamGap = Mathf.SmoothStep(0f, 0.01f, Mathf.Abs(seam - 0.5f) - 0.002f);
            return (tone + (grain - 0.5f) * 0.25f) * Mathf.Lerp(0.45f, 1f, gap * seamGap);
        }

        // Offset rows of tiles, each darker toward its lower edge.
        private static float ShinglesPixel(float x, float y)
        {
            const int rows = 8, columns = 6;
            float r = y * rows;
            int row = Mathf.FloorToInt(r);
            float c = x * columns + (row % 2) * 0.5f;
            int column = Mathf.FloorToInt(c);
            float inRow = r - row, inColumn = c - column;
            float tone = 0.75f + Hash(row * 31 + column) * 0.2f;
            float edge = Mathf.SmoothStep(0f, 0.25f, inRow);
            float side = Mathf.SmoothStep(0f, 0.04f, Mathf.Min(inColumn, 1f - inColumn));
            return tone * Mathf.Lerp(0.55f, 1f, edge * side) + (Fbm(x, y, 32, 2) - 0.5f) * 0.1f;
        }

        // Vertical straw streaks in overlapping bands.
        private static float ThatchPixel(float x, float y)
        {
            float streak = Fbm(x * 16f, y, 2, 3);
            float band = Mathf.Repeat(y * 5f, 1f);
            return 0.7f + (streak - 0.5f) * 0.6f - Mathf.SmoothStep(0.8f, 1f, band) * 0.25f;
        }

        private static float FabricPixel(float x, float y)
        {
            float weave = Mathf.Sin(x * Mathf.PI * 128f) * Mathf.Sin(y * Mathf.PI * 128f);
            return 0.9f + weave * 0.04f + (Fbm(x, y, 16, 3) - 0.5f) * 0.12f;
        }

        private static float PlasterPixel(float x, float y)
        {
            return 0.88f + (Fbm(x, y, 8, 5) - 0.5f) * 0.22f;
        }

        // Frond: bright centre vein, leaflet stripes across the width, slightly darker edges.
        private static float LeafPixel(float x, float y)
        {
            float vein = 1f - Mathf.SmoothStep(0f, 0.04f, Mathf.Abs(x - 0.5f));
            float leaflets = 0.5f + 0.5f * Mathf.Sin((y * 24f + Mathf.Abs(x - 0.5f) * 6f) * Mathf.PI * 2f);
            float edge = 1f - Mathf.Abs(x - 0.5f) * 0.5f;
            return (0.72f + leaflets * 0.12f) * edge + vein * 0.2f;
        }

        // Tileable fractal value noise; 'period' is the lattice size of the first octave.
        private static float Fbm(float x, float y, int period, int octaves)
        {
            float sum = 0f, amplitude = 0.5f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(x * period, y * period, period) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                period *= 2;
            }

            return sum / total;
        }

        private static float ValueNoise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Lattice(x0, y0, period), b = Lattice(x0 + 1, y0, period);
            float c = Lattice(x0, y0 + 1, period), d = Lattice(x0 + 1, y0 + 1, period);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Lattice(int x, int y, int period)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            return Hash(x * 73856093 ^ y * 19349663);
        }

        private static float Hash(int n)
        {
            unchecked
            {
                uint h = (uint)n * 747796405u + 2891336453u;
                h = ((h >> (int)((h >> 28) + 4u)) ^ h) * 277803737u;
                h = (h >> 22) ^ h;
                return h / (float)uint.MaxValue;
            }
        }
    }
}
