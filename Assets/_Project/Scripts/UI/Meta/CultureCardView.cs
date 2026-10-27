using BeachTycoon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// One culture on the start screen, shown through its character: portrait, name, who they are and their
    /// ability, with the country's flag and name. Highlighted while picked.
    /// </summary>
    public sealed class CultureCardView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Image background = null;
        [SerializeField] private RawImage portrait = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private TMP_Text label = null;
        [SerializeField] private TMP_Text characterName = null;
        [SerializeField] private TMP_Text ability = null;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pickedColor = new Color(0.55f, 0.82f, 1f);

        public string CultureId { get; private set; } = string.Empty;
        public Button Button => button;
        public string Label => label.text;
        public string CharacterName => characterName.text;
        public string AbilityText => ability.text;
        public Texture PortraitImage => portrait.texture;
        public bool IsPicked { get; private set; }

        public void Show(CultureDefinition culture, Texture flagImage, Texture portraitImage)
        {
            CultureId = culture.id;
            label.text = culture.displayName;
            flag.texture = flagImage;
            CharacterDefinition character = culture.character;
            characterName.text = character != null ? character.displayName : culture.displayName;
            ability.text = character != null ? $"{character.tagline}\n<size=85%>{character.ability}</size>" : string.Empty;
            portrait.texture = portraitImage;
            portrait.gameObject.SetActive(portraitImage != null);
            SetPicked(false);
        }

        public void SetPicked(bool picked)
        {
            IsPicked = picked;
            background.color = picked ? pickedColor : normalColor;
        }
    }
}
