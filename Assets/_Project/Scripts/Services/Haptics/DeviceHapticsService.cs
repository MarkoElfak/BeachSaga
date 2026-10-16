using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Vibration through the platform: short Android vibrations whose length follows the strength, and the
    /// standard iOS vibration for heavy pulses only (finer taps need a native plugin). Nothing in the editor.
    /// </summary>
    public sealed class DeviceHapticsService : IHapticsService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private bool _vibratorFailed;
#endif

        public int PulseCount { get; private set; }
        public HapticStrength LastStrength { get; private set; }

        public void Play(HapticStrength strength)
        {
            if (strength == HapticStrength.None) return;
            PulseCount++;
            LastStrength = strength;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!TryVibrateAndroid(strength == HapticStrength.Light ? 12L : strength == HapticStrength.Medium ? 25L : 45L)
                && strength == HapticStrength.Heavy)
            {
                Handheld.Vibrate(); // fallback; this call also adds the VIBRATE permission to Android builds
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (strength == HapticStrength.Heavy) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool TryVibrateAndroid(long milliseconds)
        {
            if (_vibratorFailed) return false;
            try
            {
                if (_vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }

                _vibrator.Call("vibrate", milliseconds);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Vibration unavailable: " + exception.Message);
                _vibratorFailed = true;
                return false;
            }
        }
#endif
    }
}
