using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Paints a flat head-and-shoulders picture of a character or worker from their look (rows bottom to top, like Texture2D).
    /// Shapes are laid back to front and each pixel averages four samples for smooth edges.
    /// </summary>
    public static class PortraitPainter
    {
        public static Color32[] Paint(CharacterDefinition character, PaletteDefinition palette, int size) =>
            Paint(PortraitLook.Of(character, palette), size);

        public static Color32[] Paint(PortraitLook look, int size)
        {
            var pixels = new Color32[size * size];
            float step = 1f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x * step, v = y * step;
                    Color sum = look.At(u + step * 0.25f, v + step * 0.25f) + look.At(u + step * 0.75f, v + step * 0.25f)
                        + look.At(u + step * 0.25f, v + step * 0.75f) + look.At(u + step * 0.75f, v + step * 0.75f);
                    pixels[y * size + x] = sum * 0.25f;
                }
            }

            return pixels;
        }
    }

    /// <summary>Colours and shapes of a portrait: from a character's look, or a worker's palette keys.</summary>
    public readonly struct PortraitLook
    {
        private static readonly Color Ink = new Color(0.2f, 0.13f, 0.1f);
        private static readonly Color FlowerHeart = new Color(1f, 0.85f, 0.25f);

        private readonly Color _background, _skin, _hair, _shirt, _hat;
        private readonly bool _longHair;
        private readonly CharacterHeadwear _headwear;

        public PortraitLook(Color background, Color skin, Color hair, Color shirt, bool longHair,
            CharacterHeadwear headwear = CharacterHeadwear.None, Color hat = default)
        {
            _background = background;
            _skin = skin;
            _hair = hair;
            _shirt = shirt;
            _hat = hat;
            _longHair = longHair;
            _headwear = headwear;
        }

        public static PortraitLook Of(CharacterDefinition c, PaletteDefinition palette) =>
            new PortraitLook(c.portraitBackground, palette.GetColor(c.skin), palette.GetColor(c.hair), palette.GetColor(c.shirt),
                c.longHair, c.headwear, palette.GetColor(c.headwearColor));

        public static PortraitLook Of(WorkerModel w, PaletteDefinition palette, Color background) =>
            new PortraitLook(background, palette.GetColor(w.skin), palette.GetColor(w.hair), palette.GetColor(w.shirt), w.longHair);

        // (x, y) from the bottom-left corner, 0..1.
        public Color At(float x, float y)
        {
            Color c = _background;
            if (_longHair && Ellipse(x, y, 0.5f, 0.42f, 0.27f, 0.3f)) c = _hair; // behind the head
            if (Ellipse(x, y, 0.5f, 0f, 0.42f, 0.3f)) c = _shirt;
            if (x > 0.44f && x < 0.56f && y > 0.2f && y < 0.36f) c = _skin; // neck
            if (Ellipse(x, y, 0.5f, 0.5f, 0.2f, 0.21f)) c = _skin;
            if (y > 0.57f && Ellipse(x, y, 0.5f, 0.56f, 0.215f, 0.17f)) c = _hair; // fringe
            if (Ellipse(x, y, 0.43f, 0.51f, 0.022f, 0.028f) || Ellipse(x, y, 0.57f, 0.51f, 0.022f, 0.028f)) c = Ink;
            if (y < 0.44f && Ring(x, y, 0.5f, 0.46f, 0.06f, 0.075f)) c = Ink; // smile
            return Headwear(x, y, c);
        }

        private Color Headwear(float x, float y, Color c)
        {
            switch (_headwear)
            {
                case CharacterHeadwear.Cap:
                    if (y > 0.63f && Ellipse(x, y, 0.5f, 0.63f, 0.22f, 0.11f)) c = _hat;
                    if (x > 0.5f && x < 0.8f && y > 0.615f && y < 0.655f) c = _hat; // brim
                    break;
                case CharacterHeadwear.ChefHat:
                    if (x > 0.33f && x < 0.67f && y > 0.62f && y < 0.74f) c = _hat;
                    if (Ellipse(x, y, 0.5f, 0.8f, 0.21f, 0.13f)) c = _hat;
                    break;
                case CharacterHeadwear.Flower:
                    if (Ellipse(x, y, 0.67f, 0.68f, 0.065f, 0.065f)) c = _hat;
                    if (Ellipse(x, y, 0.67f, 0.68f, 0.025f, 0.025f)) c = FlowerHeart;
                    break;
            }

            return c;
        }

        private static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }

        private static bool Ring(float x, float y, float cx, float cy, float inner, float outer)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return d >= inner && d <= outer;
        }
    }
}
