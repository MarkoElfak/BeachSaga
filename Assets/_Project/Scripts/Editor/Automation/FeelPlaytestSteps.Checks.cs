using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The sound and feel playtest's checks.</summary>
    internal static partial class FeelPlaytestSteps
    {
        private static Vector3 _restPosition, _coinFrom;
        private static Quaternion _restRotation;
        private static int _wins, _hits, _shakes;

        private static CoinPickupView TestCoin()
        {
            foreach (CoinPickupView coin in Object.FindObjectsByType<CoinPickupView>())
            {
                if (coin.SourceId == TestCoinSource) return coin;
            }

            return null;
        }

        private static ButtonFeel CloseFeel()
        {
            foreach (Button button in SettingsPanel.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "CloseButton") return button.GetComponent<ButtonFeel>();
            }

            return null;
        }

        // The editor may run few frames in the background, so look for the bounce itself, not its moment.
        private static bool BuildingPunched()
        {
            foreach (LotView lot in Object.FindObjectsByType<LotView>())
            {
                if (!lot.Placeable.Equals(PlaceableId.Lot(0))) continue;
                foreach (ScalePunch punch in lot.GetComponentsInChildren<ScalePunch>(true))
                {
                    if (punch.GetComponentInParent<StandView>() == null) return true;
                }
            }

            return false;
        }

        private static Vector3 CoinAnchor(StandView stand)
        {
            foreach (Transform child in stand.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "CoinAnchor") return child.position;
            }

            return stand.transform.position;
        }

        private static float MusicPower => Mathf.Sqrt(Music.DayVolume * Music.DayVolume + Music.NightVolume * Music.NightVolume);
        private static float FullMusic => ServiceLocator.Get<GameDatabase>().sounds.musicVolume * Settings.MusicVolume;

        private static void CheckFirstBuildAndPopCoin()
        {
            Check(Audio.TimesPlayed(SoundId.TierUpgrade) == 1 && Haptics.PulseCount > 0,
                $"the finished tent plays the fanfare and vibrates ({Haptics.PulseCount} pulses so far)");
            _restPosition = Camera.main.transform.position;
            _restRotation = Camera.main.transform.rotation;

            _shakes = Rig.ShakeCount;
            _coinFrom = CoinAnchor(PlaytestTools.Find<StandView>());
            PlaytestTools.Find<CoinSpawner>().Spawn(TestCoinSource, _coinFrom, 25);
            Check(TestCoin() != null && TestCoin().IsPopping, "a new coin pops out of its stand");
            Check(Buildings.TryUpgrade(0), "tier 2 started");
        }

        private static void CheckCoinLandsAndCollect()
        {
            CoinPickupView coin = TestCoin();
            float ring = ServiceLocator.Get<GameDatabase>().presentation.coinRingRadius;
            Vector3 offset = coin.transform.position - _coinFrom;
            float flat = new Vector2(offset.x, offset.z).magnitude;
            Check(Mathf.Abs(flat - ring) < 0.01f, $"it lands on its ring slot {flat:0.00} m from the stand (ring {ring:0.00} m)");

            int collected = Audio.TimesPlayed(SoundId.CoinCollect);
            int pulses = Haptics.PulseCount;
            PlaytestTools.FocusCamera(coin.transform.position, 14f);
            PlaytestTools.TapWorld(coin.transform.position);
            Check(TestCoin() == null && Audio.TimesPlayed(SoundId.CoinCollect) == collected + 1, "tapping it collects it with a coin sound");
            Check(Haptics.PulseCount == pulses + 1 && Haptics.LastStrength == HapticStrength.Light, "and a light vibration");
        }

        private static void CheckTierUpgradeFeel()
        {
            Check(Audio.TimesPlayed(SoundId.TierUpgrade) == 2, "tier 2 plays the fanfare");
            Check(Rig.ShakeCount == _shakes + 1, "the camera shakes");
            Check(Confetti.ActivePieces > 0, $"confetti flies ({Confetti.ActivePieces} pieces)");
            Check(BuildingPunched(), "the new building bounces");
            PlaytestTools.CapturePortraitWithHud("m11_1_tier_confetti.png");
        }

        private static void CheckCameraRestsAndSettings()
        {
            Transform camera = Camera.main.transform;
            float moved = Vector3.Distance(camera.position, _restPosition);
            Check(moved < 0.001f && Quaternion.Angle(camera.rotation, _restRotation) < 0.01f,
                $"after the shake the camera rests exactly where it was ({moved:0.0000} m off)");

            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SettingsButton");
            SettingsPanelView panel = SettingsPanel;
            PlaytestTools.SetSlider(panel, "MusicSlider", 0f);
            Check(!Music.IsPlaying && Settings.MusicVolume == 0f, "the music slider at 0 stops the music");
            PlaytestTools.SetSlider(panel, "MusicSlider", 0.5f);
            Check(Music.IsPlaying && Mathf.Abs(MusicPower - FullMusic) < 0.001f, $"at 50% it plays again at {MusicPower:0.000}");
            PlaytestTools.SetSlider(panel, "SoundSlider", 0.8f);
            Check(Mathf.Approximately(Settings.SoundVolume, 0.8f) && panel.SoundText == "Sound: 80%", $"the sound slider reads '{panel.SoundText}'");

            PlaytestTools.Click(panel, "VibrationButton");
            Check(!Settings.VibrationOn && panel.VibrationText == "Vibration: OFF", $"the vibration button reads '{panel.VibrationText}'");
            int pulses = Haptics.PulseCount;
            int rewards = Audio.TimesPlayed(SoundId.Reward);
            Audio.Play(SoundId.Reward);
            Check(Haptics.PulseCount == pulses && Audio.TimesPlayed(SoundId.Reward) == rewards + 1, "with vibration off sounds still play, without a pulse");
            PlaytestTools.CapturePortraitWithHud("m11_2_settings.png");

            CloseFeel().OnPointerDown(new PointerEventData(EventSystem.current));
        }

        private static void CheckButtonSquash()
        {
            ButtonFeel feel = CloseFeel();
            Check(feel.IsHeld && Mathf.Approximately(feel.transform.localScale.x, feel.CurrentScale),
                $"a held button squashes to {feel.CurrentScale:0.00}");
            feel.OnPointerUp(new PointerEventData(EventSystem.current));
        }

        private static void CheckButtonSpringsBackAndNight()
        {
            ButtonFeel feel = CloseFeel();
            Check(feel.transform.localScale == Vector3.one, "released, it springs back to full size");
            int clicks = Audio.TimesPlayed(SoundId.UiClick);
            ExecuteEvents.Execute(feel.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Check(!SettingsPanel.IsOpen && Audio.TimesPlayed(SoundId.UiClick) == clicks + 1, "a tap on it clicks and closes the settings");

            Clock.SetPhase(0.75f);
            Check(Clock.IsNight, "night falls");
        }

        private static void CheckNightMusicAndOpenMiniGame()
        {
            Check(Music.DayVolume < 0.001f && Mathf.Abs(Music.NightVolume - FullMusic) < 0.001f,
                $"the night tune has faded in ({Music.NightVolume:0.000}), the day tune out");
            Clock.SetPhase(0.3f);

            MiniGamePanelView.Autopilot = true;
            MiniGamePanelView.SpeedMultiplier = 4f;
            _hits = Audio.TimesPlayed(SoundId.MiniGameHit);
            _wins = Audio.TimesPlayed(SoundId.MiniGameWin);
            Games.Open(ServiceLocator.Get<MiniGameService>().ForProduct("donuts"));
        }

        private static void CheckMiniGameWin()
        {
            int hits = Audio.TimesPlayed(SoundId.MiniGameHit) - _hits;
            Check(Games.Game.State == MiniGameState.Won && hits > 0, $"donut rush won with {hits} hit sounds");
            Check(Audio.TimesPlayed(SoundId.MiniGameWin) == _wins + 1 && Confetti.ActivePieces > 0,
                $"the win plays a jingle and throws confetti ({Confetti.ActivePieces} pieces)");
            Check(Audio.TimesPlayed(SoundId.Sale) > 0, $"sales have been ringing ({Audio.TimesPlayed(SoundId.Sale)} sale sounds)");
        }

        // A moment later the confetti is mid-air over the result.
        private static void CaptureWinAndRestart()
        {
            Check(Confetti.ActivePieces >= ServiceLocator.Get<GameDatabase>().presentation.levelConfetti, $"{Confetti.ActivePieces} pieces in the air over the result");
            PlaytestTools.CapturePortraitWithHud("m11_3_minigame_win.png");
            Games.Close();
            MiniGamePanelView.Autopilot = false;
            ServiceLocator.Get<ISaveService>().Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckAfterRestart()
        {
            Check(Mathf.Approximately(Settings.SoundVolume, 0.8f) && Mathf.Approximately(Settings.MusicVolume, 0.5f) && !Settings.VibrationOn,
                "sound 80%, music 50% and vibration off survive a restart");
            Check(Music.IsPlaying && Mathf.Abs(MusicPower - FullMusic) < 0.001f, $"the music comes back at {MusicPower:0.000}");
        }
    }
}
