using System.Text;

namespace BeachTycoon.Gameplay
{
    /// <summary>Cleans up and checks a village name typed by the player.</summary>
    public static class VillageNames
    {
        /// <summary>
        /// Trims the name and squeezes runs of spaces into one, then checks the length and the characters
        /// (letters in any language, digits, spaces and - ' . &amp;). Returns null when the name is fine, else a
        /// short message for the player.
        /// </summary>
        public static string Check(string raw, int minLength, int maxLength, out string clean)
        {
            clean = Squeeze(raw);
            if (clean.Length == 0) return "Give your village a name.";
            if (clean.Length < minLength) return $"Use at least {minLength} characters.";
            if (clean.Length > maxLength) return $"Use at most {maxLength} characters.";
            foreach (char c in clean)
            {
                if (!IsAllowed(c)) return "Use letters, numbers, spaces and - ' . & only.";
            }

            return null;
        }

        private static bool IsAllowed(char c) =>
            char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'' || c == '.' || c == '&';

        private static string Squeeze(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var text = new StringBuilder(raw.Length);
            bool space = false;
            foreach (char c in raw.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    space = true;
                    continue;
                }

                if (space) text.Append(' ');
                space = false;
                text.Append(c);
            }

            return text.ToString();
        }
    }
}
