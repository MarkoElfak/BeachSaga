using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Bakes the island's colour (RGB) + smoothness (A) texture and a fine detail normal map (sand ripples,
    /// grass bumps) over TerrainShape.TerrainRect. Colours: palette band colours by z, wet sand near the
    /// waterline, tinted sand under water, a footpath, and subtle noise variation.
    /// </summary>
    public static class TerrainTextureBaker
    {
        public static void Bake(TerrainShape shape, WorldLayoutConfig layout, TerrainConfig config,
            PaletteDefinition palette, out Texture2D albedo, out Texture2D normal)
        {
            int size = config.textureSize;
            albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false) { name = "TerrainAlbedo" };
            normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "TerrainDetailNormal" };
            var colors = new Color32[size * size];
            var normals = new Color32[size * size];

            BandSpec[] bands = layout.bands;
            var bandColors = new Color[bands.Length];
            var bandTops = new float[bands.Length + 1];
            float z0 = layout.landTopZ;
            for (int i = 0; i < bands.Length; i++)
            {
                bandColors[i] = palette.GetColor(bands[i].paletteKey);
                bandTops[i] = z0;
                z0 -= bands[i].depth;
            }

            bandTops[bands.Length] = z0;

            Color wet = palette.GetColor("wetSand");
            Color dry = palette.GetColor("drySand");
            Color grass = palette.GetColor("grass");
            Rect rect = shape.TerrainRect;
            float texel = rect.width / size;

            for (int py = 0; py < size; py++)
            {
                float z = rect.yMin + (py + 0.5f) / size * rect.height;
                for (int px = 0; px < size; px++)
                {
                    float x = rect.xMin + (px + 0.5f) / size * rect.width;
                    float h = shape.Height(x, z, false);
                    Color c = BandColor(z, bandTops, bandColors, config.bandBlend);
                    float grassWeight = Similarity(c, grass, dry);
                    float n = Mathf.PerlinNoise(x * 0.55f + 31f, z * 0.55f + 17f);
                    float n2 = Mathf.PerlinNoise(x * 2.3f + 5f, z * 2.3f + 9f);

                    c = Color.Lerp(c, config.grassVariation, grassWeight * Mathf.Clamp01(n * 1.4f - 0.3f) * 0.6f);
                    c = Color.Lerp(c, config.pathColor, PathMask(config.path, x, z) * 0.85f);
                    float smooth = Mathf.Lerp(config.drySmoothness, config.grassSmoothness, grassWeight);

                    float wetness = 1f - Mathf.Clamp01((h - config.waterLevel) / config.wetHeight);
                    c = Color.Lerp(c, wet, wetness);
                    smooth = Mathf.Lerp(smooth, config.wetSmoothness, wetness);
                    if (h < config.waterLevel)
                    {
                        float depth = Mathf.Clamp01((config.waterLevel - h) / config.deepDepthRange);
                        c = Color.Lerp(wet, wet * config.underwaterTint, Mathf.Sqrt(depth));
                    }

                    float shade = 1f + ((n - 0.5f) * 1.6f + (n2 - 0.5f) * 0.6f) * config.colorNoise;
                    colors[py * size + px] = new Color(c.r * shade, c.g * shade, c.b * shade, smooth);
                    normals[py * size + px] = DetailNormal(x, z, texel, grassWeight, wetness, config.normalStrength);
                }
            }

            albedo.SetPixels32(colors);
            albedo.wrapMode = TextureWrapMode.Clamp;
            albedo.anisoLevel = 4;
            albedo.Apply(true, true);
            normal.SetPixels32(normals);
            normal.wrapMode = TextureWrapMode.Clamp;
            normal.Apply(true, true);
        }

        // tops[i] is the top edge of band i (tops[n] = bottom of the last band). Blends across each edge.
        private static Color BandColor(float z, float[] tops, Color[] colors, float blend)
        {
            int count = colors.Length;
            if (count == 0) return Color.white;
            int band = count - 1;
            for (int i = 0; i < count; i++)
            {
                if (z > tops[i + 1]) { band = i; break; }
            }

            float toTop = tops[band] - z;
            float toBottom = z - tops[band + 1];
            if (band > 0 && toTop < blend && toTop <= toBottom)
            {
                return Color.Lerp(colors[band], colors[band - 1], EdgeBlend(-toTop, blend));
            }

            if (band < count - 1 && toBottom < blend)
            {
                return Color.Lerp(colors[band + 1], colors[band], EdgeBlend(toBottom, blend));
            }

            return colors[band];
        }

        // 0.5 on the edge, 1 a full blend width above it, 0 a full width below it.
        private static float EdgeBlend(float signedDistanceAboveEdge, float blend)
        {
            float t = Mathf.Clamp01(0.5f + signedDistanceAboveEdge / (2f * blend));
            return t * t * (3f - 2f * t);
        }

        // 1 for colours equal to 'target', 0 for colours equal to 'other'.
        private static float Similarity(Color c, Color target, Color other)
        {
            float span = Mathf.Max(0.0001f, Mathf.Abs(target.g - other.g) + Mathf.Abs(target.b - other.b));
            return Mathf.Clamp01(1f - (Mathf.Abs(c.g - target.g) + Mathf.Abs(c.b - target.b)) / span);
        }

        private static float PathMask(Rect path, float x, float z)
        {
            float edge = 0.6f;
            float wobble = (Mathf.PerlinNoise(z * 0.4f, 2.2f) - 0.5f) * 0.8f;
            float dx = Mathf.Abs(x - path.center.x + wobble) - path.width * 0.5f;
            float inZ = Mathf.Clamp01((z - path.yMin) / edge) * Mathf.Clamp01((path.yMax - z) / edge);
            return Mathf.Clamp01(-dx / edge) * inZ;
        }

        // Wind ripples on sand, small bumps on grass, nothing on wet sand.
        private static Color32 DetailNormal(float x, float z, float texel, float grass, float wetness, float strength)
        {
            float Bump(float bx, float bz)
            {
                float ripple = Mathf.Sin(bz * 3.1f + Mathf.PerlinNoise(bx * 0.35f, bz * 0.35f) * 6f) * 0.5f;
                float grain = Mathf.PerlinNoise(bx * 4.2f, bz * 4.2f);
                return Mathf.Lerp(ripple * 0.35f + grain * 0.25f, grain * 0.6f, grass) * (1f - wetness);
            }

            float dx = Bump(x + texel, z) - Bump(x - texel, z);
            float dz = Bump(x, z + texel) - Bump(x, z - texel);
            var n = new Vector3(-dx * strength, -dz * strength, 2f * texel * 10f).normalized;
            return new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f),
                (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
        }
    }
}
