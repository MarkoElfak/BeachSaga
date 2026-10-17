using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>Donut rush: the customer's wanted icing in a speech bubble with a patience bar, and the icing buttons.</summary>
    public sealed class DonutRushView : MiniGameView
    {
        public static readonly string[] IcingNames = { "Strawberry", "Chocolate", "Vanilla", "Mint", "Blueberry", "Lemon" };
        public static readonly Color[] IcingColors =
        {
            new Color(0.98f, 0.5f, 0.65f), new Color(0.45f, 0.27f, 0.18f), new Color(0.98f, 0.93f, 0.78f),
            new Color(0.55f, 0.88f, 0.65f), new Color(0.45f, 0.55f, 0.95f), new Color(1f, 0.88f, 0.35f),
        };

        private const float ButtonSpacing = 290f;
        private const float OneRowY = -400f;
        private const float TopRowY = -260f;
        private const float BottomRowY = -520f;

        [SerializeField] private Image wantedIcing = null;
        [SerializeField] private TMP_Text wantedLabel = null;
        [SerializeField] private RectTransform patienceFill = null;
        [SerializeField] private Button[] icingButtons = new Button[0];

        private readonly DonutRushGame _game = new DonutRushGame();
        private int _shownCustomer = -1;

        public override MiniGameKind Kind => MiniGameKind.DonutRush;
        public override MiniGame Game => _game;

        private void Awake()
        {
            for (int i = 0; i < icingButtons.Length; i++)
            {
                int icing = i;
                icingButtons[i].onClick.AddListener(() => _game.Choose(icing));
            }
        }

        // Up to 3 icings sit in one row in the middle of the counter; 4 to 6 in two centred rows.
        public override void OnBegin()
        {
            int choices = _game.Choices;
            int perRow = choices <= 3 ? choices : (choices + 1) / 2;
            for (int i = 0; i < icingButtons.Length; i++)
            {
                bool used = i < choices;
                SetActive(icingButtons[i].gameObject, used);
                if (!used) continue;
                int row = i / perRow;
                int inRow = Mathf.Min(perRow, choices - row * perRow);
                float x = (i % perRow - (inRow - 1) * 0.5f) * ButtonSpacing;
                float y = choices <= 3 ? OneRowY : row == 0 ? TopRowY : BottomRowY;
                ((RectTransform)icingButtons[i].transform).anchoredPosition = new Vector2(x, y);
            }

            _shownCustomer = -1;
        }

        public override void Render()
        {
            SetFill(patienceFill, _game.PatienceFraction);
            if (_shownCustomer == _game.Customer) return;
            _shownCustomer = _game.Customer;
            wantedIcing.color = IcingColors[_game.Wanted];
            wantedLabel.text = IcingNames[_game.Wanted];
        }
    }
}
