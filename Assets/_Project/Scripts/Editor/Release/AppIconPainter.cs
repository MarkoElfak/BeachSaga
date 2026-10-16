using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Paints the placeholder app icon in code, matching the game's primitive look: sky, sun, sea, sand and a
    /// striped beach umbrella. Also paints the splash logo (the same scene in a rounded square).
    /// </summary>
    internal static class AppIconPainter
    {
        private static readonly Color SkyTop = new Color(0.36f, 0.72f, 0.98f);
        private static readonly Color SkyBottom = new Color(0.68f, 0.9f, 1f);
        private static readonly Color Sun = new Color(1f, 0.84f, 0.3f);
        private static readonly Color Sea = new Color(0.12f, 0.6f, 0.82f);
        private static readonly Color Foam = new Color(0.92f, 0.98f, 1f);
        private static readonly Color Sand = new Color(0.98f, 0.85f, 0.6f);
        private static readonly Color Red = new Color(0.93f, 0.33f, 0.3f);
        private static readonly Color Pole = new Color(0.45f, 0.3f, 0.2f);

        /// <param name="rounded">Transparent rounded corners (splash logo); the store icon stays square.</param>
        public static Texture2D Paint(int size, bool rounded)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    Color color = Scene(u, v);
                    if (rounded) color.a = CornerAlpha(u, v, size);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // u, v in 0..1 with v up.
        private static Color Scene(float u, float v)
        {
            float shore = 0.3f + 0.025f * Mathf.Sin(u * 9f);
            float sea = 0.47f;
            Color color = v > sea ? Color.Lerp(SkyBottom, SkyTop, (v - sea) / (1f - sea)) : v > shore ? Sea : Sand;
            if (v > sea) color = Over(color, Sun, Disc(u, v, 0.74f, 0.76f, 0.13f));
            if (v <= sea && v > shore) color = Over(color, Foam, Band(v, shore + 0.012f, 0.012f) + 0.6f * Band(v, 0.4f + 0.01f * Mathf.Sin(u * 20f), 0.006f));
            color = Over(color, Pole, Rect(u, v, 0.395f, 0.415f, 0.16f, 0.6f));
            color = Over(color, Umbrella(u, v, out float coverage), coverage);
            return color;
        }

        // A dome of six red and white stripes, tilted a little.
        private static Color Umbrella(float u, float v, out float coverage)
        {
            const float cx = 0.4f, cy = 0.58f, radius = 0.3f, tilt = -0.18f;
            float dx = u - cx, dy = v - cy;
            float rx = dx * Mathf.Cos(tilt) - dy * Mathf.Sin(tilt);
            float ry = dx * Mathf.Sin(tilt) + dy * Mathf.Cos(tilt);
            float dome = ry / 0.55f;
            float inside = rx * rx + dome * dome < radius * radius && ry > -0.02f ? 1f : 0f;
            float scallop = ry > -0.02f - 0.03f * Mathf.Abs(Mathf.Sin((rx / radius + 1f) * Mathf.PI * 3f)) ? 1f : 0f;
            coverage = inside * scallop;
            int stripe = Mathf.FloorToInt((Mathf.Atan2(dome, rx) / Mathf.PI) * 6f);
            return stripe % 2 == 0 ? Red : Color.white;
        }

        private static float Disc(float u, float v, float cx, float cy, float r) =>
            Mathf.Clamp01((r - Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy))) * 400f);

        private static float Band(float v, float center, float half) => Mathf.Clamp01((half - Mathf.Abs(v - center)) * 400f);

        private static float Rect(float u, float v, float u0, float u1, float v0, float v1) =>
            u >= u0 && u <= u1 && v >= v0 && v <= v1 ? 1f : 0f;

        private static Color Over(Color under, Color over, float amount) => Color.Lerp(under, over, Mathf.Clamp01(amount));

        private static float CornerAlpha(float u, float v, int size)
        {
            const float corner = 0.18f;
            float cx = Mathf.Clamp(u, corner, 1f - corner), cy = Mathf.Clamp(v, corner, 1f - corner);
            float distance = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
            return Mathf.Clamp01((corner - distance) * size);
        }
    }
}
