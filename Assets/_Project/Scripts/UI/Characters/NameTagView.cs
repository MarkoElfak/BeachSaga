using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>A name floating over a head; it always faces the camera, whichever way the person turns.</summary>
    public sealed class NameTagView : MonoBehaviour, ISignLabel
    {
        [SerializeField] private TMP_Text label = null;

        private Camera _camera;

        public string Text => label.text;

        public void SetText(string text) => label.text = text;

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) transform.rotation = _camera.transform.rotation;
        }
    }
}
