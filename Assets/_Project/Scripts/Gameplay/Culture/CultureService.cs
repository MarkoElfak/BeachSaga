using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's culture and how any culture looks: its flag image, its version of a prefab, and its wall and
    /// roof colours on buildings. Also which country's own place a village may open. Images and repainted materials are made on first use and kept.
    /// </summary>
    public sealed class CultureService : IDisposable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly GameDatabase _database;
        private readonly MetaModel _meta;
        private readonly Dictionary<CultureDefinition, Texture2D> _flags = new Dictionary<CultureDefinition, Texture2D>();
        private readonly Dictionary<CultureDefinition, Dictionary<Material, Material>> _recolors =
            new Dictionary<CultureDefinition, Dictionary<Material, Material>>();

        public CultureService(GameDatabase database, MetaModel meta)
        {
            _database = database;
            _meta = meta;
        }

        public IReadOnlyList<CultureDefinition> All => _database.cultures ?? Array.Empty<CultureDefinition>();

        /// <summary>The player's culture; null until one is picked.</summary>
        public CultureDefinition Current => _database.FindCulture(_meta.cultureId);

        public CultureDefinition Find(string cultureId) => _database.FindCulture(cultureId);

        public bool IsKnown(string cultureId) => Find(cultureId) != null;

        /// <summary>Places of other countries stay closed; everything else is open to every village.</summary>
        public bool CanOpen(ProductDefinition product) => product != null && product.IsOpenTo(_meta.cultureId);

        /// <summary>The country's own place (the product carrying its id), or null.</summary>
        public ProductDefinition PlaceOf(CultureDefinition culture)
        {
            ProductDefinition[] products = _database.products;
            if (culture == null || products == null) return null;
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] != null && products[i].cultureId == culture.id) return products[i];
            }

            return null;
        }

        public Texture2D Flag(CultureDefinition culture)
        {
            if (culture == null) return null;
            if (_flags.TryGetValue(culture, out Texture2D texture)) return texture;

            Vector2Int size = _database.village.flagPixels;
            texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = "Flag " + culture.id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(FlagPainter.Paint(culture.flag, size.x, size.y));
            texture.Apply(false, true);
            _flags.Add(culture, texture);
            return texture;
        }

        public GameObject Resolve(GameObject prefab, CultureDefinition culture) =>
            culture != null ? culture.Resolve(prefab) : prefab;

        /// <summary>Repaints a building's walls and roofs in the culture's colours; the main building also raises its flag.</summary>
        public void Dress(GameObject building, CultureDefinition culture, bool mainBuilding)
        {
            if (building == null || culture == null) return;
            Repaint(building, Recolors(culture));

            var view = building.GetComponent<BuildingView>();
            GameObject pole = _database.village.flagpolePrefab;
            if (!mainBuilding || view == null || view.FlagAnchor == null || pole == null) return;
            GameObject flagpole = Object.Instantiate(pole, view.FlagAnchor, false);
            flagpole.GetComponent<FlagView>().Show(Flag(culture));
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _flags.Values) Release(texture);
            foreach (Dictionary<Material, Material> map in _recolors.Values)
            {
                foreach (Material material in map.Values) Release(material);
            }

            _flags.Clear();
            _recolors.Clear();
        }

        private static void Release(Object made)
        {
            if (Application.isPlaying) Object.Destroy(made);
            else Object.DestroyImmediate(made);
        }

        // Runs when a building appears, not per frame, so the material arrays it copies are fine.
        private static void Repaint(GameObject building, Dictionary<Material, Material> map)
        {
            if (map.Count == 0) return;
            foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && map.TryGetValue(materials[i], out Material repainted))
                    {
                        materials[i] = repainted;
                        changed = true;
                    }
                }

                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private Dictionary<Material, Material> Recolors(CultureDefinition culture)
        {
            if (_recolors.TryGetValue(culture, out Dictionary<Material, Material> map)) return map;

            map = new Dictionary<Material, Material>();
            foreach (PaletteRecolor recolor in culture.recolors ?? Array.Empty<PaletteRecolor>())
            {
                Material original = _database.palette != null ? _database.palette.GetMaterial(recolor.paletteKey) : null;
                if (original == null || map.ContainsKey(original)) continue;
                var copy = new Material(original) { name = $"{original.name} ({culture.id})" };
                copy.SetColor(BaseColor, recolor.color);
                map.Add(original, copy);
            }

            _recolors.Add(culture, map);
            return map;
        }
    }
}
