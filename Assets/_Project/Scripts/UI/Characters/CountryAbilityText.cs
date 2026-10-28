using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>How a country's ability and own place read on the culture and character cards.</summary>
    public static class CountryAbilityText
    {
        /// <summary>For example "Filoxenia: visitors stay 20% longer. Own place: Taverna (souvlaki and moussaka)."</summary>
        public static string Describe(CultureDefinition culture, ProductDefinition place, bool withMenu = true)
        {
            if (culture == null) return string.Empty;
            CultureAbility ability = culture.ability;
            string text = ability != null && ability.kind != CultureAbilityKind.None ? $"<b>{ability.name}</b>: {Effect(ability)}." : string.Empty;
            if (place == null) return text;
            string menu = !withMenu || string.IsNullOrEmpty(place.description) ? string.Empty : $" ({place.description})";
            return $"{text}{(text.Length > 0 ? " " : string.Empty)}Own place: <b>{place.displayName}</b>{menu}.";
        }

        public static string Effect(CultureAbility ability)
        {
            int percent = Mathf.RoundToInt(ability.amount * 100f);
            switch (ability.kind)
            {
                case CultureAbilityKind.LongerStays: return $"visitors stay {percent}% longer";
                case CultureAbilityKind.NightAndEvents: return $"+{percent}% coins at night and in events";
                case CultureAbilityKind.HigherPrices: return $"prices {percent}% higher";
                default: return string.Empty;
            }
        }
    }
}
