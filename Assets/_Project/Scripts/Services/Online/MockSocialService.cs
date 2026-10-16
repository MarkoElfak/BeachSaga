using System;
using System.Collections.Generic;

namespace BeachTycoon.Services
{
    /// <summary>Local stand-in for a social backend: a fixed friend list, likes and gifts kept for the session.</summary>
    public sealed class MockSocialService : ISocialService
    {
        private readonly List<FriendInfo> _friends;
        private readonly HashSet<string> _liked = new HashSet<string>();
        private readonly HashSet<string> _gifted = new HashSet<string>();

        public MockSocialService(IEnumerable<FriendInfo> friends)
        {
            _friends = new List<FriendInfo>(friends ?? new FriendInfo[0]);
        }

        public int LikesGiven => _liked.Count;
        public int GiftsSent => _gifted.Count;

        public void GetFriends(Action<IReadOnlyList<FriendInfo>> onLoaded) => onLoaded?.Invoke(_friends);

        public void SendGift(string friendId, Action<bool> onDone) =>
            onDone?.Invoke(!string.IsNullOrEmpty(friendId) && _gifted.Add(friendId));

        public void Like(string ownerId, Action<bool> onDone) =>
            onDone?.Invoke(!string.IsNullOrEmpty(ownerId) && _liked.Add(ownerId));

        public bool HasLiked(string ownerId) => ownerId != null && _liked.Contains(ownerId);
        public bool HasSentGift(string friendId) => friendId != null && _gifted.Contains(friendId);
    }
}
