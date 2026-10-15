using System.Globalization;

namespace BeachTycoon.UI
{
    /// <summary>Short coin amounts for labels: 950, 12,400, 1.2M, 3.4B. Call on change only, never per frame.</summary>
    public static class NumberFormatter
    {
        private const long Thousand = 1_000;
        private const long Million = 1_000_000;
        private const long Billion = 1_000_000_000;
        private const long ShortenFrom = 100_000;

        public static string Format(long value)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            if (value < ShortenFrom) return value.ToString("N0", culture);
            if (value < Million) return (value / (double)Thousand).ToString("0.#", culture) + "K";
            if (value < Billion) return (value / (double)Million).ToString("0.##", culture) + "M";
            return (value / (double)Billion).ToString("0.##", culture) + "B";
        }
    }
}
