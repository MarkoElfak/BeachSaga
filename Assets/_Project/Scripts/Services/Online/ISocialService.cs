using System;
using System.Collections.Generic;

namespace BeachTycoon.Services
{
    /// <summary>A friend whose island can be visited.</summary>
    public readonly struct FriendInfo
    {
        public readonly string OwnerId;
        public readonly string DisplayName;
        public readonly int Tier;

        public FriendInfo(string ownerId, string displayName, int tier)
        {
            OwnerId = ownerId;
            DisplayName = displayName;
            Tier = tier;
        }
    }

    /// <summary>Friends, gifts and likes. Callbacks keep the shape of a networked service.</summary>
    public interface ISocialService
    {
        void GetFriends(Action<IReadOnlyList<FriendInfo>> onLoaded);

        /// <summary>Sends a free gift; false when one was already sent to that friend (or it failed).</summary>
        void SendGift(string friendId, Action<bool> onDone);

        /// <summary>Likes an island; false when it was already liked.</summary>
        void Like(string ownerId, Action<bool> onDone);

        bool HasLiked(string ownerId);
        bool HasSentGift(string friendId);
    }
}
