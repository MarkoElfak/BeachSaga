using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Small runtime-generated meshes for scattered detail (rocks, grass tufts).</summary>
    public static class ProceduralMeshes
    {
        /// <summary>A lumpy low-poly rock: an octahedron subdivided twice with noisy radius, flat-shaded.</summary>
        public static Mesh Rock(int seed)
        {
            var random = new System.Random(seed);
            var points = new List<Vector3>
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
            };
            var faces = new List<int> { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            for (int pass = 0; pass < 2; pass++) faces = Subdivide(points, faces);

            for (int i = 0; i < points.Count; i++)
            {
                float lump = 0.75f + (float)random.NextDouble() * 0.45f;
                Vector3 p = points[i].normalized * lump;
                points[i] = new Vector3(p.x * 1.2f, Mathf.Max(p.y * 0.65f, -0.15f), p.z); // squat, flat bottom
            }

            return FlatShaded("Rock", points, faces);
        }

        /// <summary>A tuft of grass: thin tapered blades fanning out from the centre (render two-sided).</summary>
        public static Mesh GrassTuft(int seed, int blades = 7)
        {
            var random = new System.Random(seed);
            var vertices = new List<Vector3>();
            var faces = new List<int>();
            for (int b = 0; b < blades; b++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float lean = 0.15f + (float)random.NextDouble() * 0.35f;
                float height = 0.25f + (float)random.NextDouble() * 0.25f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = new Vector3(-dir.z, 0f, dir.x) * 0.035f;
                Vector3 root = dir * 0.05f;
                int start = vertices.Count;
                vertices.Add(root - side);
                vertices.Add(root + side);
                vertices.Add(root + dir * lean + Vector3.up * height);
                faces.Add(start); faces.Add(start + 2); faces.Add(start + 1);
            }

            var mesh = new Mesh { name = "GrassTuft" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(faces, 0);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up; // soft, sky-lit blades
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<int> Subdivide(List<Vector3> points, List<int> faces)
        {
            var cache = new Dictionary<long, int>();
            var result = new List<int>(faces.Count * 4);
            for (int f = 0; f < faces.Count; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                int ab = Midpoint(points, cache, a, b), bc = Midpoint(points, cache, b, c), ca = Midpoint(points, cache, c, a);
                result.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            return result;
        }

        private static int Midpoint(List<Vector3> points, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int index)) return index;
            points.Add(((points[a] + points[b]) * 0.5f).normalized);
            cache[key] = points.Count - 1;
            return points.Count - 1;
        }

        private static Mesh FlatShaded(string name, List<Vector3> points, List<int> faces)
        {
            var vertices = new Vector3[faces.Count];
            var triangles = new int[faces.Count];
            for (int i = 0; i < faces.Count; i++)
            {
                vertices[i] = points[faces[i]];
                triangles[i] = i;
            }

            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
