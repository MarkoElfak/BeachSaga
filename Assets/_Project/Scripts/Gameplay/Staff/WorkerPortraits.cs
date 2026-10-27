using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Gameplay
{
    /// <summary>Painted portraits of workers and job seekers, made on first use and kept until they leave.</summary>
    public sealed class WorkerPortraits : IDisposable
    {
        private const int Pixels = 128;
        private static readonly Color Sand = new Color(0.96f, 0.88f, 0.7f);

        private readonly PaletteDefinition _palette;
        private readonly CultureService _cultures;
        private readonly Dictionary<int, Texture2D> _textures = new Dictionary<int, Texture2D>();

        public WorkerPortraits(PaletteDefinition palette, CultureService cultures)
        {
            _palette = palette;
            _cultures = cultures;
        }

        public Texture2D Get(WorkerModel worker)
        {
            if (_textures.TryGetValue(worker.id, out Texture2D texture)) return texture;
            texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false)
            {
                name = "Worker " + worker.id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            CultureDefinition culture = _cultures.Find(worker.cultureId);
            Color background = culture != null && culture.character != null ? culture.character.portraitBackground : Sand;
            texture.SetPixels32(PortraitPainter.Paint(PortraitLook.Of(worker, _palette, background), Pixels));
            texture.Apply(false, true);
            _textures.Add(worker.id, texture);
            return texture;
        }

        public void Forget(int workerId)
        {
            if (!_textures.TryGetValue(workerId, out Texture2D texture)) return;
            _textures.Remove(workerId);
            Destroy(texture);
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _textures.Values) Destroy(texture);
            _textures.Clear();
        }

        private static void Destroy(Texture2D texture)
        {
            if (Application.isPlaying) Object.Destroy(texture);
            else Object.DestroyImmediate(texture);
        }
    }
}
