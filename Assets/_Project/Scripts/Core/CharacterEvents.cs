using UnityEngine;

namespace BeachTycoon.Core
{
    /// <summary>The player's character exists now (on load, or once a culture is picked); views show them.</summary>
    public readonly struct CharacterJoined
    {
    }

    /// <summary>The character was sent to a stand and is walking there.</summary>
    public readonly struct CharacterAssigned
    {
        public readonly int StandIndex;

        public CharacterAssigned(int standIndex)
        {
            StandIndex = standIndex;
        }
    }

    /// <summary>The character reached their stand and serves there (it sells faster from now on).</summary>
    public readonly struct CharacterStartedWork
    {
        public readonly int StandIndex;

        public CharacterStartedWork(int standIndex)
        {
            StandIndex = standIndex;
        }
    }

    /// <summary>The character was tapped on the beach, or their HUD button was: the UI opens their card.</summary>
    public readonly struct CharacterCardRequested
    {
    }

    /// <summary>Asks the world camera to fly in close over a point.</summary>
    public readonly struct CameraFocusRequested
    {
        public readonly Vector3 Point;

        public CameraFocusRequested(Vector3 point)
        {
            Point = point;
        }
    }
}
