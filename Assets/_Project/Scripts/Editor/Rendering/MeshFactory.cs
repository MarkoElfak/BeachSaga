using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Procedural meshes saved as assets: a curved palm trunk, an arched palm frond, cones/pyramids and a
    /// surfboard. Re-running Setup refreshes the mesh data in place, so prefab references stay valid.
    /// </summary>
    internal static class MeshFactory
    {
        public const string Folder = ProjectPaths.Root + "/Art/Meshes";

        public static Mesh PalmTrunk => Save("M_PalmTrunk", BuildTrunk());
        public static Mesh PalmFrond => Save("M_PalmFrond", BuildFrond());
        public static Mesh Umbrella => Save("M_UmbrellaCanopy", BuildCone(12, 1f, 0.35f, true));
        public static Mesh Pyramid => Save("M_Pyramid", BuildCone(4, 1f, 1f, false));
        public static Mesh Cone => Save("M_Cone", BuildCone(10, 1f, 1f, false));
        public static Mesh Surfboard => Save("M_Surfboard", BuildSurfboard());

        internal static Mesh Save(string name, Mesh built)
        {
            ProjectPaths.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            built.name = name;
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, path);
                return built;
            }

            existing.Clear();
            existing.vertices = built.vertices;
            existing.normals = built.normals;
            existing.uv = built.uv;
            existing.subMeshCount = built.subMeshCount;
            for (int i = 0; i < built.subMeshCount; i++) existing.SetTriangles(built.GetTriangles(i), i);
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(built);
            return existing;
        }

        // Tapered, gently curved trunk with ring bulges (height 3, base radius 0.16).
        private static Mesh BuildTrunk()
        {
            const int rings = 14, sides = 8;
            const float height = 3f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings;
                float bulge = 1f + 0.08f * Mathf.Abs(Mathf.Sin(t * rings * Mathf.PI));
                float radius = Mathf.Lerp(0.17f, 0.09f, t) * bulge;
                var centre = new Vector3(Mathf.Sin(t * 1.4f) * 0.55f * t, t * height, 0f);
                for (int s = 0; s <= sides; s++)
                {
                    float a = (float)s / sides * Mathf.PI * 2f;
                    vertices.Add(centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
                    uvs.Add(new Vector2((float)s / sides, t * 3f));
                }
            }

            return Finish(vertices, uvs, Tube(rings, sides));
        }

        // Arched, tapering leaf with saw-tooth leaflet edges (length 2.2, along +Z, drooping).
        private static Mesh BuildFrond()
        {
            const int segments = 12;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float width = Mathf.Sin(t * Mathf.PI) * 0.42f + 0.03f;
                float tooth = i % 2 == 0 ? 1f : 0.65f;
                float z = t * 2.2f;
                float y = Mathf.Sin(t * Mathf.PI * 0.85f) * 0.45f - t * t * 1.1f; // rises, then droops
                vertices.Add(new Vector3(-width * tooth, y - width * 0.25f, z));
                vertices.Add(new Vector3(0f, y + 0.04f, z));
                vertices.Add(new Vector3(width * tooth, y - width * 0.25f, z));
                uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(0.5f, t)); uvs.Add(new Vector2(1f, t));
                if (i == segments) break;
                int b = i * 3;
                triangles.AddRange(new[] { b, b + 3, b + 1, b + 1, b + 3, b + 4, b + 1, b + 4, b + 2, b + 2, b + 4, b + 5 });
            }

            return Finish(vertices, uvs, triangles);
        }

        // Cone or pyramid of radius 1 and the given height. Alternating submeshes give striped umbrellas.
        private static Mesh BuildCone(int sides, float radius, float height, bool stripes)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var even = new List<int>();
            var odd = new List<int>();
            for (int s = 0; s < sides; s++)
            {
                float a0 = (float)s / sides * Mathf.PI * 2f, a1 = (float)(s + 1) / sides * Mathf.PI * 2f;
                int b = vertices.Count;
                vertices.Add(new Vector3(0f, height, 0f));
                vertices.Add(new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius);
                vertices.Add(new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius);
                uvs.Add(new Vector2(0.5f, 1f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0f, 0f));
                (stripes && s % 2 == 1 ? odd : even).AddRange(new[] { b, b + 1, b + 2 });
            }

            var mesh = new Mesh { vertices = vertices.ToArray(), uv = uvs.ToArray(), subMeshCount = stripes ? 2 : 1 };
            mesh.SetTriangles(even, 0);
            if (stripes) mesh.SetTriangles(odd, 1);
            mesh.RecalculateNormals(); // separate vertices per face keep the facets crisp
            mesh.RecalculateBounds();
            return mesh;
        }

        // Rounded board outline (2 long, 0.55 wide), slightly domed on top, as a flat-bottomed slab.
        private static Mesh BuildSurfboard()
        {
            const int steps = 16;
            var outline = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float half = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(t * 2f - 1f, 2f))) * 0.27f;
                outline.Add(new Vector2(half, t * 2f - 1f));
            }

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            foreach (Vector2 p in outline) { vertices.Add(new Vector3(-p.x, 0.06f, p.y)); vertices.Add(new Vector3(p.x, 0.06f, p.y)); }
            foreach (Vector2 p in outline) { vertices.Add(new Vector3(-p.x, 0f, p.y)); vertices.Add(new Vector3(p.x, 0f, p.y)); }
            for (int i = 0; i < vertices.Count; i++) uvs.Add(new Vector2(vertices[i].x + 0.5f, vertices[i].z * 0.5f + 0.5f));

            var triangles = new List<int>();
            int bottom = (steps + 1) * 2;
            for (int i = 0; i < steps; i++)
            {
                int a = i * 2;
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); // top
                int c = bottom + a;
                triangles.AddRange(new[] { c, c + 1, c + 2, c + 1, c + 3, c + 2 }); // bottom
                triangles.AddRange(new[] { a, c, a + 2, a + 2, c, c + 2 }); // left rail
                triangles.AddRange(new[] { a + 1, a + 3, c + 1, a + 3, c + 3, c + 1 }); // right rail
            }

            return Finish(vertices, uvs, triangles);
        }

        private static List<int> Tube(int rings, int sides)
        {
            var triangles = new List<int>();
            int stride = sides + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int i = r * stride + s;
                    triangles.AddRange(new[] { i, i + stride, i + 1, i + 1, i + stride, i + stride + 1 });
                }
            }

            return triangles;
        }

        private static Mesh Finish(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
