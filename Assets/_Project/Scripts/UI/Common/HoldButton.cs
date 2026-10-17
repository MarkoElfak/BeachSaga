using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachTycoon.UI
{
    /// <summary>Reports press and release (or the finger sliding off) on a UI graphic, for hold-to-act controls.</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action Released;

        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Press();
        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        public void Press()
        {
            if (IsHeld) return;
            IsHeld = true;
            Pressed?.Invoke();
        }

        public void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            Released?.Invoke();
        }

        private void OnDisable() => Release();
    }
}
