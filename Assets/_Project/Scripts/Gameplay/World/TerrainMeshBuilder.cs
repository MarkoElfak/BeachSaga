using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.Gameplay
{
    /// <summary>Builds the island height-field mesh and the water surface mesh from a TerrainShape.</summary>
    public static class TerrainMeshBuilder
    {
        private const float NormalSampleDistance = 0.25f;

        // Ground this far above the water still gets water over it, so the shoreline stays continuous.
        private const float DryMargin = 0.05f;

        public static Mesh BuildTerrain(TerrainShape shape, float cellSize)
        {
            Rect rect = shape.TerrainRect;
            int columns = Mathf.Max(1, Mathf.CeilToInt(rect.width / cellSize));
            int rows = Mathf.Max(1, Mathf.CeilToInt(rect.height / cellSize));
            int stride = columns + 1;
            var vertices = new Vector3[stride * (rows + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var tangents = new Vector4[vertices.Length];

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    float u = (float)c / columns, v = (float)r / rows;
                    float x = rect.xMin + u * rect.width, z = rect.yMin + v * rect.height;
                    int i = r * stride + c;
                    vertices[i] = new Vector3(x, shape.Height(x, z), z);
                    normals[i] = SampleNormal(shape, x, z);
                    uvs[i] = new Vector2(u, v);
                    tangents[i] = new Vector4(1f, 0f, 0f, -1f); // bitangent = +Z, matching the baked UVs
                }
            }

            return CreateMesh("Terrain", vertices, normals, uvs, tangents, null, Grid(columns, rows));
        }

        /// <summary>
        /// Flat water grid at the water level. Vertex colour: R = shore (1 at the waterline), G = depth (0..1),
        /// B = wave mask (0 at the outer edge so the skirt joins without gaps).
        /// </summary>
        public static Mesh BuildWater(TerrainShape shape, float cellSize, float shoreRange, float deepRange, float extent)
        {
            Rect rect = shape.TerrainRect;
            int columns = Mathf.Max(1, Mathf.CeilToInt(rect.width / cellSize));
            int rows = Mathf.Max(1, Mathf.CeilToInt(rect.height / cellSize));
            int stride = columns + 1;
            int gridCount = stride * (rows + 1);
            var vertices = new Vector3[gridCount + 8];
            var colors = new Color[vertices.Length];
            var wet = new bool[gridCount];
            float y = shape.WaterLevel;

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    float x = rect.xMin + (float)c / columns * rect.width;
                    float z = rect.yMin + (float)r / rows * rect.height;
                    float signedDepth = shape.WaterDepth(x, z);
                    float depth = Mathf.Max(0f, signedDepth);
                    bool border = r == 0 || c == 0 || r == rows || c == columns;
                    int i = r * stride + c;
                    wet[i] = signedDepth > -DryMargin || border;
                    vertices[i] = new Vector3(x, y, z);
                    colors[i] = new Color(1f - Mathf.Clamp01(depth / shoreRange), Mathf.Clamp01(depth / deepRange), border ? 0f : 1f, 1f);
                }
            }

            // Skirt: an outer ring of 4 corners per side, flat and deep.
            Rect outer = new Rect(rect.xMin - extent, rect.yMin - extent, rect.width + extent * 2f, rect.height + extent * 2f);
            Vector2[] ring = { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax),
                outer.min, new Vector2(outer.xMax, outer.yMin), outer.max, new Vector2(outer.xMin, outer.yMax) };
            for (int k = 0; k < 8; k++)
            {
                vertices[gridCount + k] = new Vector3(ring[k].x, y, ring[k].y);
                colors[gridCount + k] = new Color(0f, 1f, 0f, 1f);
            }

            int[] grid = WetGrid(columns, rows, wet, out int gridLength);
            var triangles = new int[gridLength + 24];
            System.Array.Copy(grid, triangles, gridLength);
            int t = gridLength;
            for (int side = 0; side < 4; side++)
            {
                int a = gridCount + side, b = gridCount + (side + 1) % 4;
                int oa = gridCount + 4 + side, ob = gridCount + 4 + (side + 1) % 4;
                // Clockwise seen from above.
                triangles[t++] = a; triangles[t++] = ob; triangles[t++] = oa;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = ob;
            }

            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            return CreateMesh("Water", vertices, normals, null, null, colors, triangles);
        }

        private static Vector3 SampleNormal(TerrainShape shape, float x, float z)
        {
            float d = NormalSampleDistance;
            float hx = shape.Height(x + d, z) - shape.Height(x - d, z);
            float hz = shape.Height(x, z + d) - shape.Height(x, z - d);
            return new Vector3(-hx, 2f * d, -hz).normalized;
        }

        // Water cells with at least one corner in (or just under) the sea. Water hidden under the island is left out:
        // it costs fill rate on phones and must never show through the sand.
        private static int[] WetGrid(int columns, int rows, bool[] wet, out int length)
        {
            int[] grid = Grid(columns, rows);
            int stride = columns + 1;
            length = 0;
            for (int t = 0; t < grid.Length; t += 6)
            {
                int i = grid[t];
                if (!wet[i] && !wet[i + 1] && !wet[i + stride] && !wet[i + stride + 1]) continue;
                System.Array.Copy(grid, t, grid, length, 6);
                length += 6;
            }

            return grid;
        }

        private static int[] Grid(int columns, int rows)
        {
            int stride = columns + 1;
            var triangles = new int[columns * rows * 6];
            int t = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int i = r * stride + c;
                    triangles[t++] = i; triangles[t++] = i + stride; triangles[t++] = i + stride + 1;
                    triangles[t++] = i; triangles[t++] = i + stride + 1; triangles[t++] = i + 1;
                }
            }

            return triangles;
        }

        private static Mesh CreateMesh(string name, Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
            Vector4[] tangents, Color[] colors, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = normals;
            if (uvs != null) mesh.uv = uvs;
            if (tangents != null) mesh.tangents = tangents;
            if (colors != null) mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
