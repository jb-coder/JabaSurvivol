using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Short, low-amplitude haptic pulses for the opt-in accessibility setting.
    /// The implementation is a no-op outside Android and never touches the
    /// vibrator when <see cref="GameSettings.Haptics"/> is off.
    /// </summary>
    public static class HapticFeedback
    {
        private const int PulseMilliseconds = 28;
        private const int PulseAmplitude = 48;

        private static int _pulseCount;
        private static AndroidJavaObject _vibrator;

        /// <summary>
        /// Number of pulses requested while haptics were enabled. Tests use this
        /// to verify that the off state produces no pulses.
        /// </summary>
        public static int PulseCountForTests => _pulseCount;

        public static void ResetForTests()
        {
            _pulseCount = 0;
        }

        public static void PlayerDamage()
        {
            Pulse();
        }

        public static void BossSlam()
        {
            Pulse();
        }

        public static void RoundClear()
        {
            Pulse();
        }

        private static void Pulse()
        {
            if (!GameSettings.Haptics)
            {
                return;
            }

            _pulseCount++;

            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            try
            {
                var vibrator = GetVibrator();
                if (vibrator == null)
                {
                    return;
                }

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        (long)PulseMilliseconds,
                        PulseAmplitude);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", (long)PulseMilliseconds);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Haptic feedback failed: " + exception.GetType().Name);
            }
        }

        private static AndroidJavaObject GetVibrator()
        {
            if (_vibrator != null)
            {
                return _vibrator;
            }

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null)
            {
                return null;
            }

            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            return _vibrator;
        }
    }
}
