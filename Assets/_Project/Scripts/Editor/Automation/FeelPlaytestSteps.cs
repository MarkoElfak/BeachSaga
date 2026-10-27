using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 11 playtest (sound and feel): every hook has a generated sound and day music plays. Building the
    /// tent and the next tier plays sounds, shakes the camera (which then rests on its exact pose), throws
    /// confetti and bounces the building. A coin pops out of its stand and a tap collects it. The settings
    /// sliders change and mute the music, vibration can be turned off, a held button squashes and springs back,
    /// night crossfades to the night tune, a won mini-game plays hit and win sounds with confetti, and a restart
    /// keeps the settings.
    /// </summary>
    internal static partial class FeelPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const int TestCoinSource = 999;

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static AudioService Audio => ServiceLocator.Get<AudioService>();
        private static MusicPlayer Music => ServiceLocator.Get<MusicPlayer>();
        private static IHapticsService Haptics => ServiceLocator.Get<IHapticsService>();
        private static SettingsService Settings => ServiceLocator.Get<SettingsService>();
        private static DayClock Clock => ServiceLocator.Get<DayClock>();
        private static TopDownCameraController Rig => Camera.main.GetComponent<TopDownCameraController>();
        private static ConfettiView Confetti => PlaytestTools.Find<ConfettiView>();
        private static SettingsPanelView SettingsPanel => PlaytestTools.Find<SettingsPanelView>();
        private static MiniGamePanelView Games => PlaytestTools.Find<MiniGamePanelView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null && !Rig.IsShaking;
                case 2: return TestCoin() != null && !TestCoin().IsPopping;
                case 3: return Buildings.MainTier == 2;
                case 4: return !Rig.IsShaking && !Rig.IsFlying;
                case 5: return CloseFeel().CurrentScale < 0.95f;
                case 6: return !CloseFeel().IsHeld && CloseFeel().CurrentScale == 1f; // settles on exactly 1
                case 7: return Music.NightMix >= 1f;
                case 8: return Games.ShowsIntro;
                case 9: return Games.ShowsResult;
                case 10: return Confetti.SinceBurst >= 0.8f;
                case 11: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: Setup(); break;
                case 1: CheckFirstBuildAndPopCoin(); break;
                case 2: CheckCoinLandsAndCollect(); break;
                case 3: CheckTierUpgradeFeel(); break;
                case 4: CheckCameraRestsAndSettings(); break;
                case 5: CheckButtonSquash(); break;
                case 6: CheckButtonSpringsBackAndNight(); break;
                case 7: CheckNightMusicAndOpenMiniGame(); break;
                case 8: PlaytestTools.Click(Games, "PlayButton"); break;
                case 9: CheckMiniGameWin(); break;
                case 10: CaptureWinAndRestart(); break;
                case 11: CheckAfterRestart(); break;
                default: Done(); break;
            }
        }

        private static void Setup()
        {
            Time.timeScale = TestTimeScale;
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            Clock.SetPhase(0.3f);

            SoundLibrary library = ServiceLocator.Get<GameDatabase>().sounds;
            int missing = 0;
            foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
            {
                if (!library.TryGet(id, out _)) missing++;
            }

            int hooks = System.Enum.GetValues(typeof(SoundId)).Length;
            Check(missing == 0, $"all {hooks} sound hooks have a generated clip ({missing} missing)");
            Check(library.music != null && library.nightMusic != null,
                $"day and night music are assigned ({Length(library.music)} s and {Length(library.nightMusic)} s loops)");
            Check(Music.IsPlaying && Mathf.Abs(MusicPower - library.musicVolume) < 0.001f, $"music plays at {MusicPower:0.00}");
            Check(Settings.SoundVolume == 1f && Settings.MusicVolume == 1f && Settings.VibrationOn, "a new game starts at full volume with vibration on");

            ServiceLocator.Get<IEconomyService>().Earn(2000000);
            Check(Buildings.TryBuild(0), "tent started");
            Check(Audio.TimesPlayed(SoundId.BuildStart) == 1, "starting it plays the build sound");
        }

        private static void Done()
        {
            MiniGamePanelView.Autopilot = false;
            MiniGamePanelView.SpeedMultiplier = 1f;
            AutoTestRunner.Finish();
        }

        private static string Length(AudioClip clip) => clip != null ? clip.length.ToString("0.0") : "-";

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
