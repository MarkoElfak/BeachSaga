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

    /// <summary>A press stayed still for InputConfig.holdToMoveSeconds (long press).</summary>
    public readonly struct HoldStarted
    {
        public readonly Vector2 ScreenPosition;

        public HoldStarted(Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
        }
    }

    /// <summary>The pointer moved while captured by a long-press interaction (camera does not pan).</summary>
    public readonly struct CapturedPointerMoved
    {
        public readonly Vector2 ScreenPosition;

        public CapturedPointerMoved(Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
        }
    }

    /// <summary>The captured pointer was released.</summary>
    public readonly struct CapturedPointerReleased
    {
        public readonly Vector2 ScreenPosition;

        public CapturedPointerReleased(Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
        }
    }
}
