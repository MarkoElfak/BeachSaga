using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>A row of culture cards (each shown through its character) made from the game data, so a new culture shows up without UI work.</summary>
    public sealed class CulturePickerView : MonoBehaviour
    {
        [SerializeField] private CultureCardView cardTemplate = null;
        [SerializeField] private Transform cardRow = null;
        [Tooltip("The \"Choose your character\" line over the cards.")]
        [SerializeField] private GameObject label = null;

        private readonly List<CultureCardView> _cards = new List<CultureCardView>();

        /// <summary>Raised when a card is tapped, with the culture id.</summary>
        public event Action<string> Picked;

        public string PickedId { get; private set; } = string.Empty;
        public int CardCount => _cards.Count;

        public CultureCardView Card(int index) => _cards[index];

        /// <summary>Hidden when the page heading already asks for the culture.</summary>
        public void ShowLabel(bool show) => label.SetActive(show);

        public void Build(CultureService cultures, CharacterService characters)
        {
            cardTemplate.gameObject.SetActive(false);
            if (_cards.Count > 0) return;

            IReadOnlyList<CultureDefinition> all = cultures.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) continue;
                CultureCardView card = Instantiate(cardTemplate, cardRow, false);
                card.name = "Card " + all[i].id;
                card.gameObject.SetActive(true);
                card.Show(all[i], cultures.Flag(all[i]), characters != null ? characters.Portrait(all[i].character) : null);
                string id = all[i].id;
                card.Button.onClick.AddListener(() => Pick(id));
                _cards.Add(card);
            }
        }

        public void Pick(string cultureId)
        {
            Select(cultureId);
            Picked?.Invoke(cultureId);
        }

        /// <summary>Highlights a card without announcing it (empty = none).</summary>
        public void Select(string cultureId)
        {
            PickedId = cultureId ?? string.Empty;
            for (int i = 0; i < _cards.Count; i++) _cards[i].SetPicked(_cards[i].CultureId == PickedId);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null) _cards[i].Button.onClick.RemoveAllListeners();
            }
        }
    }
}
