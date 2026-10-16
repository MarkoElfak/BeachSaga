using System.Collections.Generic;
using System.IO;
using System.Text;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Measures prefabs for placement: the ground rectangle (x, z relative to the pivot) covered by their visible
    /// meshes, vertex-exact. Progress bars, other floating UI and the manager hat are ignored. Setup stores the results in the
    /// tier, product, house and decoration data (where Auto Footprint is on) and writes a readable report.
    /// </summary>
    [InitializeOnLoad]
    internal static class FootprintMeasure
    {
        private const float Step = 0.05f;
        private const string RequestPath = "Logs/BeachTycoonFootprints.request";
        public const string ReportPath = "Logs/BeachTycoonFootprints.txt";

        static FootprintMeasure()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            WriteReport();
        }

        /// <summary>Measures every placeable prefab into its data asset (only where Auto Footprint is on).</summary>
        public static void Bake(GameDatabase database)
        {
            foreach (TierDefinition tier in database.tiers ?? new TierDefinition[0])
            {
                if (tier != null && tier.autoFootprint) Store(tier, ref tier.footprint, Measure(tier.buildingPrefab));
            }

            foreach (ProductDefinition product in database.products ?? new ProductDefinition[0])
            {
                if (product == null || !product.autoFootprint) continue;
                Store(product, ref product.standFootprint, Measure(product.standPrefab));
                product.queueStart = QueueStart(product.standPrefab);
            }

            foreach (DecorationDefinition decoration in database.decorations ?? new DecorationDefinition[0])
            {
                if (decoration != null && decoration.autoFootprint) Store(decoration, ref decoration.area, Measure(decoration.prefab));
            }

            if (database.world != null && database.world.autoHouseFootprint && database.neighbours != null)
            {
                // Houses share one lot size: the largest of the variants.
                bool any = false;
                var union = new Rect();
                foreach (GameObject house in database.neighbours.housePrefabs ?? new GameObject[0])
                {
                    Rect r = Measure(house);
                    if (r.width <= 0f) continue;
                    union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, r.xMin), Mathf.Min(union.yMin, r.yMin),
                        Mathf.Max(union.xMax, r.xMax), Mathf.Max(union.yMax, r.yMax)) : r;
                    any = true;
                }

                Store(database.world, ref database.world.houseFootprint, union);
            }
        }

        private static void Store(Object owner, ref Rect field, Rect measured)
        {
            if (measured.width <= 0f || measured.height <= 0f) return; // no prefab: keep the configured size
            field = measured;
            EditorUtility.SetDirty(owner);
        }

        [MenuItem("Beach Tycoon/Report Footprints", priority = 60)]
        public static void WriteReport()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataAssetBuilder.DatabasePath);
            if (database == null) return;
            var report = new StringBuilder("Footprints (x, z, width, depth relative to the pivot)\n");
            foreach (TierDefinition tier in database.tiers ?? new TierDefinition[0])
            {
                if (tier != null) report.Append($"tier {tier.id}: {Describe(Measure(tier.buildingPrefab))}\n");
            }

            foreach (ProductDefinition product in database.products ?? new ProductDefinition[0])
            {
                if (product == null) continue;
                report.Append($"stand {product.id}: {Describe(Measure(product.standPrefab))}");
                report.Append($" queue {QueueStart(product.standPrefab)}\n");
            }

            foreach (GameObject house in database.neighbours != null ? database.neighbours.housePrefabs : new GameObject[0])
            {
                report.Append($"house {(house != null ? house.name : "-")}: {Describe(Measure(house))}\n");
            }

            foreach (DecorationDefinition decoration in database.decorations ?? new DecorationDefinition[0])
            {
                if (decoration != null) report.Append($"decoration {decoration.id}: {Describe(Measure(decoration.prefab))}\n");
            }

            File.WriteAllText(ReportPath, report.ToString());
        }

        /// <summary>Ground rectangle of the prefab's meshes, rounded outward to 5 cm. Zero size when empty.</summary>
        public static Rect Measure(GameObject prefab)
        {
            if (prefab == null) return new Rect();
            var instance = (GameObject)Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            try
            {
                // Vertex-exact (renderer bounds inflate rotated parts such as 45-degree roofs).
                bool any = false;
                float xMin = float.MaxValue, zMin = float.MaxValue, xMax = float.MinValue, zMax = float.MinValue;
                var filters = new List<MeshFilter>();
                instance.GetComponentsInChildren(true, filters);
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh == null || IsIgnored(filter.transform)) continue;
                    if (filter.GetComponent<MeshRenderer>() == null) continue;
                    Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
                    foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 p = toWorld.MultiplyPoint3x4(vertex);
                        xMin = Mathf.Min(xMin, p.x);
                        xMax = Mathf.Max(xMax, p.x);
                        zMin = Mathf.Min(zMin, p.z);
                        zMax = Mathf.Max(zMax, p.z);
                        any = true;
                    }
                }

                if (!any) return new Rect();
                return Rect.MinMaxRect(Mathf.Floor(xMin / Step) * Step, Mathf.Floor(zMin / Step) * Step,
                    Mathf.Ceil(xMax / Step) * Step, Mathf.Ceil(zMax / Step) * Step);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // Floating UI and the manager's hat (it perches on the roof) do not take ground space.
        private static bool IsIgnored(Transform part)
        {
            if (part.GetComponentInParent<ProgressBarView>(true) != null) return true;
            for (Transform t = part; t != null; t = t.parent)
            {
                if (t.name == StandPrefabs.ManagerHatName) return true;
            }

            return false;
        }

        /// <summary>Local x/z of the StandView's queue anchor (where visitors line up).</summary>
        public static Vector2 QueueStart(GameObject standPrefab)
        {
            StandView view = standPrefab != null ? standPrefab.GetComponent<StandView>() : null;
            if (view == null) return Vector2.zero;
            var anchor = new SerializedObject(view).FindProperty("queueAnchor").objectReferenceValue as Transform;
            return anchor != null ? new Vector2(anchor.localPosition.x, anchor.localPosition.z) : Vector2.zero;
        }

        private static string Describe(Rect r) => $"x {r.xMin:0.00}..{r.xMax:0.00}  z {r.yMin:0.00}..{r.yMax:0.00}  ({r.width:0.00} x {r.height:0.00})";
    }
}
