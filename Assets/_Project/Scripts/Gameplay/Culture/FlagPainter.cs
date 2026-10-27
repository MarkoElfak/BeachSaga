using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Draws a FlagDesign into pixels (rows bottom to top, like Texture2D).</summary>
    public static class FlagPainter
    {
        public static Color32[] Paint(FlagDesign flag, int width, int height)
        {
            var pixels = new Color32[width * height];
            Paint(flag, width, height, pixels);
            return pixels;
        }

        public static void Paint(FlagDesign flag, int width, int height, Color32[] pixels)
        {
            float cantonWidth = flag.cantonSize.x * width;
            float cantonHeight = flag.cantonSize.y * height;
            float halfBar = flag.crossWidth * cantonHeight * 0.5f;
            for (int y = 0; y < height; y++)
            {
                float fromTop = height - y - 0.5f;
                for (int x = 0; x < width; x++)
                {
                    float fromLeft = x + 0.5f;
                    Color color;
                    if (flag.canton && fromLeft < cantonWidth && fromTop < cantonHeight)
                    {
                        // The cross bars have the same thickness both ways, whatever the canton's shape.
                        bool bar = Mathf.Abs(fromTop - cantonHeight * 0.5f) < halfBar || Mathf.Abs(fromLeft - cantonWidth * 0.5f) < halfBar;
                        color = bar ? flag.crossColor : flag.cantonColor;
                    }
                    else
                    {
                        float along = flag.vertical ? fromLeft / width : fromTop / height;
                        color = Stripe(flag, along);
                    }

                    pixels[y * width + x] = color;
                }
            }
        }

        /// <summary>The stripe colour at a point 0..1 across the stripes (from the top, or from the left when vertical).</summary>
        public static Color Stripe(FlagDesign flag, float along)
        {
            Color[] stripes = flag.stripes;
            if (stripes == null || stripes.Length == 0) return Color.white;

            float total = 0f;
            for (int i = 0; i < stripes.Length; i++) total += Weight(flag, i);
            float edge = 0f;
            for (int i = 0; i < stripes.Length; i++)
            {
                edge += Weight(flag, i) / total;
                if (along < edge) return stripes[i];
            }

            return stripes[stripes.Length - 1];
        }

        private static float Weight(FlagDesign flag, int stripe) =>
            flag.stripeWeights != null && stripe < flag.stripeWeights.Length ? Mathf.Max(0.01f, flag.stripeWeights[stripe]) : 1f;
    }
}
