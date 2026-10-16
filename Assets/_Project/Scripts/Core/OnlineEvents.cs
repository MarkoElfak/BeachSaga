namespace BeachTycoon.Core
{
    /// <summary>The player is about to leave their own beach for a visit (views bank waiting coins).</summary>
    public readonly struct LeavingHome
    {
    }

    /// <summary>A visit began; Bonus is the visit coin bonus paid (0 when already paid today).</summary>
    public readonly struct VisitStarted
    {
        public readonly string OwnerId;
        public readonly long Bonus;

        public VisitStarted(string ownerId, long bonus)
        {
            OwnerId = ownerId;
            Bonus = bonus;
        }
    }

    /// <summary>The island could not be loaded (missing or unreadable snapshot); the player stays home.</summary>
    public readonly struct VisitFailed
    {
        public readonly string OwnerId;

        public VisitFailed(string ownerId)
        {
            OwnerId = ownerId;
        }
    }

    /// <summary>A like or a gift on the visited island went through (or was refused).</summary>
    public readonly struct SocialActionDone
    {
        public readonly bool IsGift;
        public readonly bool Success;

        public SocialActionDone(bool isGift, bool success)
        {
            IsGift = isGift;
            Success = success;
        }
    }
}
