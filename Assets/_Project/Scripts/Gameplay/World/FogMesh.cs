using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Flat grid for one fog layer over a piece of land. Vertex alpha is 1 well inside the piece and fades out
    /// over 'inside' metres before its edge and 'outside' metres past it; the faded rim droops a little.
    /// </summary>
    public static class FogMesh
    {
        private const float EdgeDroop = 0.6f;

        public static Mesh Build(Rect area, float inside, float outside, float cellSize)
        {
            Rect rect = LandMap.Grow(area, outside);
            int columns = Mathf.Max(1, Mathf.CeilToInt(rect.width / cellSize));
            int rows = Mathf.Max(1, Mathf.CeilToInt(rect.height / cellSize));
            int stride = columns + 1;
            var vertices = new Vector3[stride * (rows + 1)];
            var colors = new Color[vertices.Length];
            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    float x = rect.xMin + rect.width * c / columns;
                    float z = rect.yMin + rect.height * r / rows;
                    float depth = Mathf.Min(Mathf.Min(x - area.xMin, area.xMax - x), Mathf.Min(z - area.yMin, area.yMax - z));
                    float alpha = Smooth(-outside, inside, depth);
                    vertices[r * stride + c] = new Vector3(x, (alpha - 1f) * EdgeDroop, z);
                    colors[r * stride + c] = new Color(1f, 1f, 1f, alpha);
                }
            }

            var triangles = new int[columns * rows * 6];
            int t = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int i = r * stride + c;
                    triangles[t++] = i;
                    triangles[t++] = i + stride;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + stride;
                    triangles[t++] = i + stride + 1;
                }
            }

            var mesh = new Mesh { name = "Fog" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Smooth(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
