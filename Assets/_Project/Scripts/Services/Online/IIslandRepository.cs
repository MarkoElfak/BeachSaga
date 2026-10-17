using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Where island snapshots live. Calls complete through callbacks so a server version can answer later;
    /// the local version answers immediately.
    /// </summary>
    public interface IIslandRepository
    {
        /// <summary>Stores the snapshot under its ownerId; false when it could not be written or is too big.</summary>
        void SaveSnapshot(IslandSnapshot snapshot, Action<bool> onDone);

        /// <summary>Latest snapshot of that owner, or null when there is none or it is unreadable.</summary>
        void LoadSnapshot(string ownerId, Action<IslandSnapshot> onLoaded);
    }
}
