using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Perfect pour: a glass with the green target band and the drink rising while the button is held.</summary>
    public sealed class PerfectPourView : MiniGameView
    {
        [SerializeField] private RectTransform fill = null;
        [SerializeField] private RectTransform band = null;
        [SerializeField] private HoldButton pourButton = null;
        [SerializeField] private TMP_Text result = null;

        private readonly PerfectPourGame _game = new PerfectPourGame();
        private bool _shownResting;

        public override MiniGameKind Kind => MiniGameKind.PerfectPour;
        public override MiniGame Game => _game;

        private void Awake()
        {
            pourButton.Pressed += _game.StartPour;
            pourButton.Released += _game.StopPour;
        }

        private void OnDestroy()
        {
            pourButton.Pressed -= _game.StartPour;
            pourButton.Released -= _game.StopPour;
        }

        public override void OnBegin() => _shownResting = true; // forces the result text off on the first frame

        public override void Render()
        {
            fill.anchorMax = new Vector2(1f, _game.Fill);
            band.anchorMin = new Vector2(0f, _game.BandMin);
            band.anchorMax = new Vector2(1f, _game.BandMax);
            if (_shownResting == _game.Resting) return;
            _shownResting = _game.Resting;
            SetActive(result.gameObject, _game.Resting);
            if (_game.Resting) result.text = _game.LastGood ? "Perfect!" : "Oops!";
        }
    }
}
