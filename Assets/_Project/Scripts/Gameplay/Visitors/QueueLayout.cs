using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Where each queue slot stands relative to a stand's first queue spot: short lines side by side, walked
    /// like a snake (the second line runs back towards the stand), so moving up is only a short step.
    /// </summary>
    public static class QueueLayout
    {
        /// <summary>(sideways, away from the stand) offset of a slot. The last line takes any overflow.</summary>
        public static Vector2 Slot(int slot, VisitorConfig config)
        {
            int perLine = Mathf.Max(1, config.queuePerLine);
            int lines = Mathf.Max(1, config.queueLines);
            int line = Mathf.Min(slot / perLine, lines - 1);
            int along = slot - line * perLine;
            if (line % 2 == 1 && along < perLine) along = perLine - 1 - along;
            float side = (line - (lines - 1) * 0.5f) * config.queueLineSpacing;
            return new Vector2(side, along * config.queueSpacing);
        }

        /// <summary>Width of the strip all lines need, given the width one line keeps free.</summary>
        public static float Width(VisitorConfig config, float lineWidth) =>
            lineWidth + (Mathf.Max(1, config.queueLines) - 1) * config.queueLineSpacing;

        /// <summary>How far the lines reach away from the stand.</summary>
        public static float Length(VisitorConfig config) => Mathf.Max(1, config.queuePerLine) * config.queueSpacing;
    }
}
