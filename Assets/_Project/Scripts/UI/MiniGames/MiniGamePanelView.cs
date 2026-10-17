using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The stand's mini-game window: an intro (how to play, level, reward), the game itself with score, lives and
    /// time, and the result. Games tick on unscaled time in small fixed steps. Autopilot makes a game play itself
    /// (playtests, the debug menu); SpeedMultiplier fast-forwards it.
    /// </summary>
    public sealed partial class MiniGamePanelView : MonoBehaviour
    {
        public static bool Autopilot;
        public static float SpeedMultiplier = 1f;

        private const float StepSeconds = 1f / 60f;
        private const float MaxFrameSeconds = 0.1f;

        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text title = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private GameObject introPage = null;
        [SerializeField] private TMP_Text howToPlay = null;
        [SerializeField] private TMP_Text levelInfo = null;
        [SerializeField] private TMP_Text rewardInfo = null;
        [SerializeField] private Button playButton = null;
        [SerializeField] private TMP_Text playLabel = null;
        [SerializeField] private GameObject gamePage = null;
        [SerializeField] private TMP_Text scoreText = null;
        [SerializeField] private TMP_Text livesText = null;
        [SerializeField] private GameObject timerBar = null;
        [SerializeField] private RectTransform timerFill = null;
        [SerializeField] private GameObject resultPage = null;
        [SerializeField] private TMP_Text resultTitle = null;
        [SerializeField] private TMP_Text resultBody = null;
        [SerializeField] private Button againButton = null;
        [SerializeField] private TMP_Text againLabel = null;
        [SerializeField] private Button backButton = null;
        [SerializeField] private MiniGameView[] views = new MiniGameView[0];

        private MiniGameService _games;
        private StandService _stands;
        private MiniGameView _view;
        private int _shownScore = -1;
        private int _shownLives = -1;

        public bool IsOpen => root.activeSelf;
        public MiniGameDefinition Definition { get; private set; }
        public int LevelIndex { get; private set; }
        public MiniGameView ActiveView => _view;
        public MiniGame Game => _view != null ? _view.Game : null;
        public bool IsPlaying => IsOpen && gamePage.activeSelf && Game != null && Game.State == MiniGameState.Playing;
        public bool ShowsIntro => IsOpen && introPage.activeSelf;
        public bool ShowsResult => IsOpen && resultPage.activeSelf;

        /// <summary>The last finished run passed a level for the first time.</summary>
        public bool LastRunRewarded { get; private set; }

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _games)) return;
            _stands = ServiceLocator.Get<StandService>();
            closeButton.onClick.AddListener(Close);
            playButton.onClick.AddListener(Play);
            againButton.onClick.AddListener(PlayNext);
            backButton.onClick.AddListener(ShowIntro);
            EventBus.Subscribe<MiniGameRequested>(OnRequested);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Close);
            playButton.onClick.RemoveListener(Play);
            againButton.onClick.RemoveListener(PlayNext);
            backButton.onClick.RemoveListener(ShowIntro);
            EventBus.Unsubscribe<MiniGameRequested>(OnRequested);
        }

        public void Open(MiniGameDefinition game)
        {
            Definition = game;
            root.SetActive(true);
            ShowIntro();
        }

        /// <summary>Closing mid-game abandons the run (no reward, nothing lost).</summary>
        public void Close() => root.SetActive(false);

        public void Play()
        {
            LevelIndex = _games.NextLevelIndex(Definition);
            _view = null;
            for (int i = 0; i < views.Length; i++)
            {
                bool match = views[i].Kind == Definition.kind;
                SetActive(views[i].gameObject, match);
                if (match) _view = views[i];
            }

            ShowPage(gamePage);
            Game.Begin(Definition.levels[LevelIndex], System.Environment.TickCount);
            _view.OnBegin();
            _shownScore = _shownLives = -1;
            SetActive(timerBar, Game.HasTimeLimit);
            Draw();
        }

        // A run can also end between frames, on the player's own tap; the result shows on the next frame either way.
        private void Update()
        {
            if (!IsOpen || !gamePage.activeSelf || Game == null) return;
            float left = Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds) * Mathf.Max(0f, SpeedMultiplier);
            while (left > 0f && !Game.IsOver)
            {
                float step = Mathf.Min(left, StepSeconds);
                if (Autopilot) Game.AutoPlay();
                Game.Tick(step);
                left -= step;
            }

            Draw();
            if (Game.IsOver) ShowResult();
        }

        private void Draw()
        {
            _view.Render();
            if (Game.HasTimeLimit) MiniGameView.SetFill(timerFill, Game.TimeLeft / Game.Level.timeLimit);
            if (_shownScore != Game.Score)
            {
                if (_shownScore >= 0 && Game.Score > _shownScore) EventBus.Publish(new SoundRequested(SoundId.MiniGameHit));
                scoreText.text = $"{(_shownScore = Game.Score)} / {Game.Target}";
            }

            if (_shownLives != Game.LivesLeft)
            {
                if (_shownLives >= 0 && Game.LivesLeft < _shownLives) EventBus.Publish(new SoundRequested(SoundId.MiniGameMiss));
                livesText.text = LivesText(_shownLives = Game.LivesLeft);
            }
        }

        private void ShowIntro()
        {
            ShowPage(introPage);
            title.text = Definition.title;
            howToPlay.text = Definition.howToPlay;
            FillIntro();
        }

        private void ShowResult()
        {
            bool won = Game.State == MiniGameState.Won;
            LastRunRewarded = _games.ReportResult(Definition, LevelIndex, won);
            EventBus.Publish(new SoundRequested(won ? SoundId.MiniGameWin : SoundId.MiniGameLose));
            ShowPage(resultPage);
            FillResult(won);
        }

        private void PlayNext() => Play();

        private void OnRequested(MiniGameRequested evt)
        {
            if (evt.StandIndex < 0 || evt.StandIndex >= _stands.Count) return;
            MiniGameDefinition game = _games.ForProduct(_stands.Get(evt.StandIndex).Model.productId);
            if (game != null && game.LevelCount > 0) Open(game);
        }

        private void ShowPage(GameObject page)
        {
            SetActive(introPage, page == introPage);
            SetActive(gamePage, page == gamePage);
            SetActive(resultPage, page == resultPage);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target.activeSelf != active) target.SetActive(active);
        }
    }
}
