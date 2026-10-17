using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Towel run: a grid of sunbeds; a visitor and their patience bar show on a bed while they wait.</summary>
    public sealed class TowelRunView : MiniGameView
    {
        [SerializeField] private RectTransform area = null;
        [SerializeField] private Button[] beds = new Button[0];
        [SerializeField] private GameObject[] visitors = new GameObject[0];
        [SerializeField] private RectTransform[] waitFills = new RectTransform[0];

        private readonly TowelRunGame _game = new TowelRunGame();

        public override MiniGameKind Kind => MiniGameKind.TowelRun;
        public override MiniGame Game => _game;

        private void Awake()
        {
            for (int i = 0; i < beds.Length; i++)
            {
                int bed = i;
                beds[i].onClick.AddListener(() => _game.Tap(bed));
            }
        }

        // Two columns for up to four beds, otherwise three, centred in the area.
        public override void OnBegin()
        {
            int count = Mathf.Min(_game.Beds, beds.Length);
            int columns = count <= 4 ? 2 : 3;
            int rows = (count + columns - 1) / columns;
            Vector2 cell = new Vector2(area.rect.width / columns, area.rect.height / Mathf.Max(rows, 3));
            for (int i = 0; i < beds.Length; i++)
            {
                SetActive(beds[i].gameObject, i < count);
                if (i >= count) continue;
                int column = i % columns, row = i / columns;
                var rect = (RectTransform)beds[i].transform;
                rect.anchoredPosition = new Vector2((column - (columns - 1) * 0.5f) * cell.x, ((rows - 1) * 0.5f - row) * cell.y);
            }
        }

        public override void Render()
        {
            int count = Mathf.Min(_game.Beds, beds.Length);
            for (int i = 0; i < count; i++)
            {
                bool waiting = _game.IsWaiting(i);
                SetActive(visitors[i], waiting);
                if (waiting) SetFill(waitFills[i], _game.WaitFraction(i));
            }
        }
    }
}
