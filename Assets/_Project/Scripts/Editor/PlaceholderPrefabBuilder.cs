using System;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Builds every procedural placeholder prefab. Existing prefabs are kept unless overwrite is requested,
    /// so swapped-in art is never replaced by accident.
    /// </summary>
    internal static class PlaceholderPrefabBuilder
    {
        private const string Buildings = ProjectPaths.Prefabs + "/Buildings/";
        private const string Stands = ProjectPaths.Prefabs + "/Stands/";
        public const string TentPath = Buildings + "Tent.prefab";
        public const string CabinPath = Buildings + "Cabin.prefab";
        public const string BungalowPath = Buildings + "Bungalow.prefab";
        public const string HotelPath = Buildings + "Hotel.prefab";
        public const string ResortPath = Buildings + "Resort.prefab";
        public const string DonutStandPath = Stands + "DonutStand.prefab";
        public const string TowelStandPath = Stands + "TowelStand.prefab";
        public const string DrinksBarPath = Stands + "DrinksBar.prefab";
        public const string SurfSchoolPath = Stands + "SurfSchool.prefab";
        public const string RealEstatePath = Stands + "RealEstateOffice.prefab";
        public const string LotPath = ProjectPaths.Prefabs + "/World/Lot.prefab";
        public const string PalmPath = ProjectPaths.Prefabs + "/World/Palm.prefab";
        public const string CoinPath = ProjectPaths.Prefabs + "/Effects/CoinPickup.prefab";
        public const string FlagpolePath = ProjectPaths.Prefabs + "/World/Flagpole.prefab";
        public const string LandSignPath = ProjectPaths.Prefabs + "/World/LandSign.prefab";
        public const string VisitorFolder = ProjectPaths.Prefabs + "/Visitors";
        public const string HouseFolder = ProjectPaths.Prefabs + "/Houses";
        public const string DecorationFolder = ProjectPaths.Prefabs + "/Decorations";

        public static readonly (string name, Func<PaletteDefinition, GameObject> build)[] Decorations =
        {
            ("BeachUmbrella", DecorationPrefabs.BuildUmbrella),
            ("DeckChairs", DecorationPrefabs.BuildDeckChairs),
            ("Sandcastle", DecorationPrefabs.BuildSandcastle),
            ("VolleyballNet", DecorationPrefabs.BuildVolleyball),
            ("LifeguardTower", DecorationPrefabs.BuildLifeguardTower),
        };

        public static void BuildAll(PaletteDefinition p, bool overwrite)
        {
            Build(TentPath, overwrite, () => BuildingPrefabs.BuildTent(p));
            Build(CabinPath, overwrite, () => BuildingPrefabs.BuildCabin(p));
            Build(BungalowPath, overwrite, () => BuildingPrefabs.BuildBungalow(p));
            Build(HotelPath, overwrite, () => TallBuildingPrefabs.BuildHotel(p));
            Build(ResortPath, overwrite, () => TallBuildingPrefabs.BuildResort(p));
            Build(DonutStandPath, overwrite, () => StandPrefabs.BuildDonutStand(p));
            Build(TowelStandPath, overwrite, () => StandPrefabs.BuildTowelStand(p));
            Build(DrinksBarPath, overwrite, () => StandPrefabs.BuildDrinksBar(p));
            Build(SurfSchoolPath, overwrite, () => StandPrefabs.BuildSurfSchool(p));
            Build(RealEstatePath, overwrite, () => StandPrefabs.BuildRealEstate(p));
            Build(LotPath, overwrite, () => WorldPrefabs.BuildLot(p));
            Build(PalmPath, overwrite, () => WorldPrefabs.BuildPalm(p));
            Build(CoinPath, overwrite, () => WorldPrefabs.BuildCoin(p));
            Build(FlagpolePath, overwrite, () => FlagpoleBuilder.Build(p));
            Build(LandSignPath, overwrite, () => LandSignBuilder.Build(p).gameObject);
            foreach (VisitorPrefabs.Look look in VisitorPrefabs.Looks)
            {
                Build(VisitorPath(look.Name), overwrite, () => VisitorPrefabs.Build(look, p));
            }

            for (int i = 0; i < HousePrefabs.Variants.Length; i++)
            {
                int variant = i;
                Build(HousePath(i), overwrite, () => HousePrefabs.Build(variant, p));
            }

            foreach (var (name, build) in Decorations)
            {
                Build(DecorationPath(name), overwrite, () => build(p));
            }
        }

        public static string VisitorPath(string name) => $"{VisitorFolder}/{name}.prefab";
        public static string HousePath(int variant) => $"{HouseFolder}/{HousePrefabs.Variants[variant].name}.prefab";
        public static string DecorationPath(string name) => $"{DecorationFolder}/{name}.prefab";

        public static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void Build(string path, bool overwrite, Func<GameObject> create)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            ProjectPaths.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            PrimitiveKit.SavePrefab(create(), path);
        }
    }
}
