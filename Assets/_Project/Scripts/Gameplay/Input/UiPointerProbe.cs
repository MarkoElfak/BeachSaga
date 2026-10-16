using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachTycoon.Gameplay
{
    /// <summary>Answers "is this screen point over UI?" with reused buffers (no per-call allocations).</summary>
    public sealed class UiPointerProbe
    {
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private PointerEventData _pointer;
        private EventSystem _owner;

        public bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointer == null || _owner != eventSystem)
            {
                _pointer = new PointerEventData(eventSystem);
                _owner = eventSystem;
            }

            _pointer.position = screenPosition;
            _hits.Clear();
            eventSystem.RaycastAll(_pointer, _hits);
            return _hits.Count > 0;
        }
    }
}
