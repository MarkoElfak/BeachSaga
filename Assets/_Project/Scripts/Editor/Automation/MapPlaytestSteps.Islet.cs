using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    // The islet once it opens: visitors reach its towels only over the boardwalk, and walk on the deck above the sea.
    internal static partial class MapPlaytestSteps
    {
        private static void CheckIsletPaths()
        {
            PlaytestTools.CapturePortraitWithHud("m16_8_resort_whole.png");
            var navigation = ServiceLocator.Get<WorldNavigation>();
            var random = new System.Random(3);
            var reserved = new List<int>();
            int islet = -1;
            for (int spot = navigation.ReserveSpot(random); spot >= 0; spot = navigation.ReserveSpot(random))
            {
                reserved.Add(spot);
                if (navigation.GetSpot(spot).z > 20f) islet = spot;
            }

            foreach (int spot in reserved) navigation.ReleaseSpot(spot);
            if (islet < 0) islet = IsletSpotInUse(navigation); // a busy beach: visitors may hold every free islet towel
            Check(islet >= 0, $"towel spots on the islet are open to visitors ({reserved.Count} free spots)");
            if (islet < 0) return;

            var path = new List<Vector3>();
            navigation.BuildPath(navigation.GetEntry(0), navigation.GetSpot(islet), path);
            Vector2 from = Map.pieces[PieceFor(TopTier)].boardwalkFrom, to = Map.pieces[PieceFor(TopTier)].boardwalkTo;
            Check(path.Exists(p => Near(p, from)) && path.Exists(p => Near(p, to)), $"the walk to the islet crosses the boardwalk ({path.Count} waypoints)");
            float deck = PlaytestTools.Find<BeachWorldBuilder>().Shape.WalkHeight(0f, 10f);
            Check(deck > PlaytestTools.Find<BeachWorldBuilder>().Shape.WaterLevel, $"visitors walk on the deck above the sea ({deck:0.00} m)");
        }

        private static int IsletSpotInUse(WorldNavigation navigation)
        {
            IReadOnlyList<VisitorAgent> visitors = ServiceLocator.Get<VisitorService>().Active;
            for (int i = 0; i < visitors.Count; i++)
            {
                int spot = visitors[i].SpotIndex;
                if (spot >= 0 && navigation.GetSpot(spot).z > 20f) return spot;
            }

            return -1;
        }

        private static void CheckVisitorOnIslet()
        {
            VisitorAgent visitor = VisitorOnIslet();
            Check(visitor != null, $"a visitor walked out to the islet ({visitor?.Position})");
            if (visitor == null) return;
            PlaytestTools.FocusCamera(new Vector3(visitor.Position.x, 0f, visitor.Position.z - 2f), 18f);
            PlaytestTools.CapturePortrait("m16_9_islet_visitor.png");
        }

        private static VisitorAgent VisitorOnIslet()
        {
            IReadOnlyList<VisitorAgent> active = ServiceLocator.Get<VisitorService>().Active;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Position.z > 14f) return active[i];
            }

            return null;
        }

        private static bool Near(Vector3 point, Vector2 target) => Vector2.Distance(new Vector2(point.x, point.z), target) < 0.01f;
    }
}
