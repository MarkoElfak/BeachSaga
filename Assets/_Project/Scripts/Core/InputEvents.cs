using UnityEngine;

namespace BeachTycoon.Core
{
    /// <summary>Short press-and-release without movement, in screen pixels.</summary>
    public readonly struct TapEvent
    {
        public readonly Vector2 ScreenPosition;

        public TapEvent(Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
        }
    }

    /// <summary>One-finger (or mouse) drag step between two screen positions.</summary>
    public readonly struct DragEvent
    {
        public readonly Vector2 From;
        public readonly Vector2 To;

        public DragEvent(Vector2 from, Vector2 to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>Zoom step. Ratio &gt; 1 means fingers moved apart (zoom in).</summary>
    public readonly struct PinchEvent
    {
        public readonly float Ratio;
        public readonly Vector2 ScreenCenter;

        public PinchEvent(float ratio, Vector2 screenCenter)
        {
            Ratio = ratio;
            ScreenCenter = screenCenter;
        }
    }
}
