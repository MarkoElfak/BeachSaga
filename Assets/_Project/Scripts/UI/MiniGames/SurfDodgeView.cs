using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Surf dodge: a strip of sea with lanes, rolling wave lines, rocks and buoys coming down and the board at the
    /// bottom (it glides between lanes and blinks while safe after a hit). Left and right buttons steer.
    /// </summary>
    public sealed class SurfDodgeView : MiniGameView
    {
        private const float BoardGlide = 14f;

        [SerializeField] private RectTransform sea = null;
        [SerializeField] private RectTransform board = null;
        [SerializeField] private Image boardImage = null;
        [SerializeField] private Button leftButton = null;
        [SerializeField] private Button rightButton = null;
        [SerializeField] private RectTransform[] laneLines = new RectTransform[0];
        [SerializeField] private RectTransform[] waves = new RectTransform[0];
        [SerializeField] private Image[] obstacles = new Image[0];
        [SerializeField] private Color rockColor = new Color(0.45f, 0.42f, 0.4f);
        [SerializeField] private Color buoyColor = new Color(0.95f, 0.35f, 0.3f);

        private readonly SurfDodgeGame _game = new SurfDodgeGame();
        private float _boardX;
        private int _shownObstacles;

        public override MiniGameKind Kind => MiniGameKind.SurfDodge;
        public override MiniGame Game => _game;

        private float LaneWidth => sea.rect.width / _game.Lanes;

        private void Awake()
        {
            leftButton.onClick.AddListener(() => _game.Steer(-1));
            rightButton.onClick.AddListener(() => _game.Steer(1));
        }

        public override void OnBegin()
        {
            for (int i = 0; i < laneLines.Length; i++)
            {
                bool used = i < _game.Lanes - 1;
                SetActive(laneLines[i].gameObject, used);
                if (used) laneLines[i].anchoredPosition = new Vector2(LaneX(i) + LaneWidth * 0.5f, 0f);
            }

            _boardX = LaneX(_game.BoardLane);
            _shownObstacles = obstacles.Length;
        }

        public override void Render()
        {
            float height = sea.rect.height;
            _boardX = Mathf.Lerp(_boardX, LaneX(_game.BoardLane), Mathf.Clamp01(Time.unscaledDeltaTime * BoardGlide));
            board.anchoredPosition = new Vector2(_boardX, SurfDodgeGame.BoardY * height);
            boardImage.enabled = _game.SafeLeft <= 0f || Mathf.Repeat(_game.SafeLeft, 0.2f) > 0.1f;

            float scroll = Mathf.Repeat(_game.Elapsed * _game.Level.speed, 1f);
            for (int i = 0; i < waves.Length; i++)
            {
                float y = Mathf.Repeat((float)i / waves.Length - scroll, 1f);
                waves[i].anchoredPosition = new Vector2(waves[i].anchoredPosition.x, y * height);
            }

            int used = 0;
            for (int row = 0; row < SurfDodgeGame.MaxRows; row++)
            {
                if (!_game.IsRowActive(row)) continue;
                for (int lane = 0; lane < _game.Lanes && used < obstacles.Length; lane++)
                {
                    if (!_game.Blocks(row, lane)) continue;
                    Image obstacle = obstacles[used++];
                    SetActive(obstacle.gameObject, true);
                    obstacle.rectTransform.anchoredPosition = new Vector2(LaneX(lane), _game.RowY(row) * height);
                    obstacle.color = (row + lane) % 3 == 0 ? buoyColor : rockColor;
                }
            }

            for (int i = used; i < _shownObstacles; i++) SetActive(obstacles[i].gameObject, false);
            _shownObstacles = used;
        }

        // Centre of a lane, measured from the middle of the sea strip.
        private float LaneX(int lane) => (lane + 0.5f) * LaneWidth - sea.rect.width * 0.5f;
    }
}
