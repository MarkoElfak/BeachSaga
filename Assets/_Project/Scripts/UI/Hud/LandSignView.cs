using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>The tall sign standing in a locked piece's fog: its name and what opens it.</summary>
    public sealed class LandSignView : MonoBehaviour, ISignLabel
    {
        [SerializeField] private TMP_Text label = null;

        public string Text => label.text;

        public void SetText(string text) => label.text = text;
    }
}
