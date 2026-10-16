using System;

namespace BeachTycoon.Services
{
    /// <summary>Who the player is. The local version is always signed in; a backend one would sign in remotely.</summary>
    public interface IAuthService
    {
        bool IsSignedIn { get; }

        /// <summary>Stable owner id of this player's island snapshots.</summary>
        string PlayerId { get; }
        string DisplayName { get; }

        void SignIn(Action<bool> onDone);
    }
}
