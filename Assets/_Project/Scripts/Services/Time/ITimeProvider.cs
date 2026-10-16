using System;

namespace BeachTycoon.Services
{
    /// <summary>Wall-clock source, swappable in tests (offline earnings, save timestamps).</summary>
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
