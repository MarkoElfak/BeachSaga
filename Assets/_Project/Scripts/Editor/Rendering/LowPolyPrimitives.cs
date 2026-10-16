using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Low-poly, CPU-readable stand-ins for Unity's primitive meshes (same size and pivot). They cut the
    /// triangle count of visitors and props several times over and, being readable, let runtime scenery be
    /// static batched. Built once per editor session and saved next to MeshFactory's meshes.
    /// </summary>
    internal static class LowPolyPrimitives
    {
        private const int Sides = 12;
        private const int SphereRings = 8;
        private const int CapRings = 4;

        private static readonly Dictionary<PrimitiveType, Mesh> Cache = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>The replacement mesh for a primitive, or null to keep Unity's (planes and quads).</summary>
        public static Mesh For(PrimitiveType type)
        {
            if (Cache.TryGetValue(type, out Mesh cached) && cached != null) return cached;
            Mesh mesh;
            switch (type)
            {
                case PrimitiveType.Sphere: mesh = MeshFactory.Save("LP_Sphere", Lathe(SphereProfile(), false)); break;
                case PrimitiveType.Capsule: mesh = MeshFactory.Save("LP_Capsule", Lathe(CapsuleProfile(), true)); break;
                case PrimitiveType.Cylinder: mesh = MeshFactory.Save("LP_Cylinder", Cylinder()); break;
                case PrimitiveType.Cube: mesh = MeshFactory.Save("LP_Cube", Cube()); break;
                default: return null;
            }

            Cache[type] = mesh;
            return mesh;
        }

        // (radius, height) points from bottom pole to top pole.
        private static List<Vector2> SphereProfile()
        {
            var points = new List<Vector2>();
            for (int r = 0; r <= SphereRings; r++)
            {
                float a = Mathf.PI * r / SphereRings - Mathf.PI * 0.5f;
                points.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f));
            }

            return points;
        }

        // Unity's capsule: height 2, radius 0.5, hemispheres centred at y = +-0.5.
        private static List<Vector2> CapsuleProfile()
        {
            var points = new List<Vector2>();
            for (int r = 0; r <= CapRings; r++)
            {
                float a = Mathf.PI * 0.5f * r / CapRings - Mathf.PI * 0.5f;
                points.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f - 0.5f));
            }

            for (int r = 0; r <= CapRings; r++)
            {
                float a = Mathf.PI * 0.5f * r / CapRings;
                points.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }

            return points;
        }

        // Revolves a profile around Y with smooth normals: from the centre, or for a capsule from the centre of
        // the nearer hemisphere (the straight middle gets horizontal normals).
        private static Mesh Lathe(List<Vector2> profile, bool capsule)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int r = 0; r < profile.Count; r++)
            {
                float centreY = capsule ? Mathf.Sign(profile[r].y) * 0.5f : 0f;
                for (int s = 0; s <= Sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / Sides;
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var point = direction * profile[r].x + Vector3.up * profile[r].y;
                    vertices.Add(point);
                    normals.Add((point - Vector3.up * centreY).normalized);
                    uvs.Add(new Vector2((float)s / Sides, (float)r / (profile.Count - 1)));
                }
            }

            int stride = Sides + 1;
            for (int r = 0; r < profile.Count - 1; r++)
            {
                for (int s = 0; s < Sides; s++)
                {
                    int a = r * stride + s, b = a + stride;
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            }

            return Build(vertices, normals, uvs, triangles);
        }

        // Height 2 (y -1..1), radius 0.5, flat caps.
        private static Mesh Cylinder()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int s = 0; s <= Sides; s++)
            {
                float angle = 2f * Mathf.PI * s / Sides;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (int end = 0; end < 2; end++)
                {
                    vertices.Add(direction * 0.5f + Vector3.up * (end == 0 ? -1f : 1f));
                    normals.Add(direction);
                    uvs.Add(new Vector2((float)s / Sides, end));
                }
            }

            for (int s = 0; s < Sides; s++)
            {
                int a = s * 2;
                triangles.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
            }

            for (int end = 0; end < 2; end++)
            {
                float y = end == 0 ? -1f : 1f;
                int centre = vertices.Count;
                vertices.Add(Vector3.up * y);
                normals.Add(Vector3.up * y);
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (int s = 0; s <= Sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / Sides;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, y, Mathf.Sin(angle) * 0.5f));
                    normals.Add(Vector3.up * y);
                    uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
                }

                for (int s = 0; s < Sides; s++)
                {
                    if (end == 1) triangles.AddRange(new[] { centre, centre + s + 2, centre + s + 1 });
                    else triangles.AddRange(new[] { centre, centre + s + 1, centre + s + 2 });
                }
            }

            return Build(vertices, normals, uvs, triangles);
        }

        // Unit cube centred on the origin, one quad (4 vertices) per face.
        private static Mesh Cube()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in faces)
            {
                Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 side = Vector3.Cross(up, normal);
                int start = vertices.Count;
                for (int i = 0; i < 4; i++)
                {
                    float u = i == 1 || i == 2 ? 1f : 0f;
                    float v = i >= 2 ? 1f : 0f;
                    vertices.Add((normal + side * (u * 2f - 1f) + up * (v * 2f - 1f)) * 0.5f);
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, v));
                }

                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            return Build(vertices, normals, uvs, triangles);
        }

        private static Mesh Build(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
