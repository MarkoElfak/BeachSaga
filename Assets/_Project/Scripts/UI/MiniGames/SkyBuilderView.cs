using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Sky builder: the tower's top floors on the ground, the next floor sliding above them between two guide
    /// marks (the drop window), and a floor that missed falling away. Tapping anywhere drops the floor.
    /// </summary>
    public sealed class SkyBuilderView : MiniGameView
    {
        [SerializeField] private RectTransform area = null;
        [SerializeField] private Button dropButton = null;
        [SerializeField] private RectTransform[] floors = new RectTransform[0];
        [SerializeField] private RectTransform slider = null;
        [SerializeField] private RectTransform falling = null;
        [SerializeField] private RectTransform[] guides = new RectTransform[2];
        [SerializeField] private float floorHeight = 110f;
        [SerializeField] private float groundY = 60f;

        private readonly SkyBuilderGame _game = new SkyBuilderGame();
        private int _shownFloors = -1;
        private float _guideX;

        public override MiniGameKind Kind => MiniGameKind.SkyBuilder;
        public override MiniGame Game => _game;

        private float SlideHalf => (area.rect.width - slider.rect.width) * 0.5f;

        private void Awake() => dropButton.onClick.AddListener(_game.Drop);

        public override void OnBegin()
        {
            _shownFloors = -1;
            _guideX = _game.Level.tolerance * SlideHalf + slider.rect.width * 0.5f; // floor edges at the window's ends
        }

        public override void Render()
        {
            int visible = Mathf.Min(_game.Score, floors.Length);
            if (_shownFloors != visible)
            {
                _shownFloors = visible;
                for (int i = 0; i < floors.Length; i++)
                {
                    SetActive(floors[i].gameObject, i < visible);
                    floors[i].anchoredPosition = new Vector2(0f, groundY + floorHeight * (i + 0.5f));
                }
            }

            float top = groundY + floorHeight * (visible + 1.5f);
            bool fell = _game.Resting && !_game.LastGood;
            SetActive(slider.gameObject, !_game.Resting);
            SetActive(falling.gameObject, fell);
            slider.anchoredPosition = new Vector2(_game.FloorX * SlideHalf, top);
            guides[0].anchoredPosition = new Vector2(-_guideX, top);
            guides[1].anchoredPosition = new Vector2(_guideX, top);
            if (!fell) return;
            float drop = 1f - _game.RestLeft / SkyBuilderGame.RestSeconds;
            falling.anchoredPosition = new Vector2(_game.LastX * SlideHalf, top - drop * drop * floorHeight * 5f);
        }
    }
}
