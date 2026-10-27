using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Instance matrices (one cube mesh) for an island's boardwalk: deck planks across its width, posts down to the
    /// ground or sea floor every couple of metres, and a rail along each side.
    /// </summary>
    public static class BoardwalkBuilder
    {
        private const float PlankStep = 0.5f;
        private const float PlankDepth = 0.42f;
        private const float PlankThickness = 0.07f;
        private const float PostStep = 2f;
        private const float PostSize = 0.16f;
        private const float RailHeight = 0.6f;
        private const float RailSize = 0.08f;

        public static void Add(MapConfig map, in LandPiece piece, TerrainShape ground, List<Matrix4x4> into)
        {
            if (!LandMap.HasBoardwalk(piece)) return;
            var from = new Vector3(piece.boardwalkFrom.x, 0f, piece.boardwalkFrom.y);
            var to = new Vector3(piece.boardwalkTo.x, 0f, piece.boardwalkTo.y);
            Vector3 along = to - from;
            float length = along.magnitude;
            Quaternion turn = Quaternion.LookRotation(along / length);
            Vector3 side = turn * Vector3.right * (map.boardwalkWidth * 0.5f);
            float deck = ground.WaterLevel + map.boardwalkHeight;

            for (float d = PlankStep * 0.5f; d < length; d += PlankStep)
            {
                Vector3 p = from + along * (d / length);
                into.Add(Matrix4x4.TRS(new Vector3(p.x, deck - PlankThickness * 0.5f, p.z), turn,
                    new Vector3(map.boardwalkWidth, PlankThickness, PlankDepth)));
            }

            for (float d = 0f; d <= length + 0.01f; d += PostStep)
            {
                Vector3 p = from + along * Mathf.Min(1f, d / length);
                AddPost(p + side, ground, deck, turn, into);
                AddPost(p - side, ground, deck, turn, into);
            }

            Vector3 middle = (from + to) * 0.5f + Vector3.up * (deck + RailHeight);
            var rail = new Vector3(RailSize, RailSize, length);
            into.Add(Matrix4x4.TRS(middle + side, turn, rail));
            into.Add(Matrix4x4.TRS(middle - side, turn, rail));
        }

        // From the ground (or the sea floor) up to the rail.
        private static void AddPost(Vector3 at, TerrainShape ground, float deck, Quaternion turn, List<Matrix4x4> into)
        {
            float bottom = ground.Height(at.x, at.z, false);
            float top = deck + RailHeight;
            into.Add(Matrix4x4.TRS(new Vector3(at.x, (bottom + top) * 0.5f, at.z), turn, new Vector3(PostSize, top - bottom, PostSize)));
        }
    }
}
