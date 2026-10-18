using BeachTycoon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>One culture on the start screen: its flag and name; highlighted while picked.</summary>
    public sealed class CultureCardView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Image background = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private TMP_Text label = null;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pickedColor = new Color(0.55f, 0.82f, 1f);

        public string CultureId { get; private set; } = string.Empty;
        public Button Button => button;
        public string Label => label.text;
        public bool IsPicked { get; private set; }

        public void Show(CultureDefinition culture, Texture flagImage)
        {
            CultureId = culture.id;
            label.text = culture.displayName;
            flag.texture = flagImage;
            SetPicked(false);
        }

        public void SetPicked(bool picked)
        {
            IsPicked = picked;
            background.color = picked ? pickedColor : normalColor;
        }
    }
}
