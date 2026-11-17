using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Aim assist levels from the touch control design note.
    /// </summary>
    public enum AimAssistLevel
    {
        Off,
        Low,
        High
    }

    /// <summary>
    /// Device-local settings introduced for the v0.9.0 touch and accessibility
    /// options. Values use <see cref="PlayerPrefs"/> and never touch the
    /// persistent profile or cloud save schema.
    /// </summary>
    public static class GameSettings
    {
        public const string LeftHandedKey = "settings.leftHanded";
        public const string JoystickSizeKey = "settings.joystickSize";
        public const string JoystickOpacityKey = "settings.joystickOpacity";
        public const string AimAssistKey = "settings.aimAssist";
        public const string AutoFireKey = "settings.autoFire";
        public const string ReducedMotionKey = "settings.reducedMotion";
        public const string HapticsKey = "settings.haptics";
        public const string ColorblindSafeTelegraphsKey = "settings.colorblindSafeTelegraphs";

        public const bool DefaultLeftHanded = false;
        public const float DefaultJoystickSize = 1f;
        public const float DefaultJoystickOpacity = 1f;
        public const AimAssistLevel DefaultAimAssist = AimAssistLevel.Off;
        public const bool DefaultAutoFire = true;
        public const bool DefaultReducedMotion = false;
        public const bool DefaultHaptics = false;
        public const bool DefaultColorblindSafeTelegraphs = false;

        public const float MinJoystickSize = 0.8f;
        public const float MaxJoystickSize = 1.4f;
        public const float JoystickSizeStep = 0.1f;
        public const float MinJoystickOpacity = 0.4f;
        public const float MaxJoystickOpacity = 1f;
        public const float JoystickOpacityStep = 0.1f;

        private static bool _loaded;
        private static bool _leftHanded;
        private static float _joystickSize;
        private static float _joystickOpacity;
        private static AimAssistLevel _aimAssist;
        private static bool _autoFire;
        private static bool _reducedMotion;
        private static bool _haptics;
        private static bool _colorblindSafeTelegraphs;

        /// <summary>
        /// True when the touch controls are mirrored for left-handed play.
        /// </summary>
        public static bool LeftHanded
        {
            get
            {
                EnsureLoaded();
                return _leftHanded;
            }
        }

        /// <summary>
        /// Joystick scale in the range 0.8 to 1.4.
        /// </summary>
        public static float JoystickSize
        {
            get
            {
                EnsureLoaded();
                return _joystickSize;
            }
        }

        /// <summary>
        /// Joystick visual opacity in the range 0.4 to 1.0.
        /// </summary>
        public static float JoystickOpacity
        {
            get
            {
                EnsureLoaded();
                return _joystickOpacity;
            }
        }

        /// <summary>
        /// Optional aim assist level. Off by default.
        /// </summary>
        public static AimAssistLevel AimAssist
        {
            get
            {
                EnsureLoaded();
                return _aimAssist;
            }
        }

        /// <summary>
        /// True when the weapon fires continuously while aiming. On by default.
        /// </summary>
        public static bool AutoFire
        {
            get
            {
                EnsureLoaded();
                return _autoFire;
            }
        }

        /// <summary>
        /// True when non-essential motion should be reduced. Off by default.
        /// </summary>
        public static bool ReducedMotion
        {
            get
            {
                EnsureLoaded();
                return _reducedMotion;
            }
        }

        /// <summary>
        /// True when supported devices may use short haptic feedback. Off by default.
        /// </summary>
        public static bool Haptics
        {
            get
            {
                EnsureLoaded();
                return _haptics;
            }
        }

        /// <summary>
        /// True when combat telegraphs add a non-color cue. Off by default.
        /// </summary>
        public static bool ColorblindSafeTelegraphs
        {
            get
            {
                EnsureLoaded();
                return _colorblindSafeTelegraphs;
            }
        }

        public static void SetLeftHanded(bool value)
        {
            EnsureLoaded();
            _leftHanded = value;
            PlayerPrefs.SetInt(LeftHandedKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetJoystickSize(float value)
        {
            EnsureLoaded();
            _joystickSize = ClampJoystickSize(value);
            PlayerPrefs.SetFloat(JoystickSizeKey, _joystickSize);
            PlayerPrefs.Save();
        }

        public static void SetJoystickOpacity(float value)
        {
            EnsureLoaded();
            _joystickOpacity = ClampJoystickOpacity(value);
            PlayerPrefs.SetFloat(JoystickOpacityKey, _joystickOpacity);
            PlayerPrefs.Save();
        }

        public static void SetAimAssist(AimAssistLevel value)
        {
            EnsureLoaded();
            _aimAssist = IsValidAimAssist(value) ? value : DefaultAimAssist;
            PlayerPrefs.SetInt(AimAssistKey, (int)_aimAssist);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Advances Off to Low, Low to High, and High back to Off.
        /// </summary>
        public static AimAssistLevel CycleAimAssist()
        {
            EnsureLoaded();
            var next = _aimAssist switch
            {
                AimAssistLevel.Off => AimAssistLevel.Low,
                AimAssistLevel.Low => AimAssistLevel.High,
                _ => AimAssistLevel.Off
            };
            SetAimAssist(next);
            return next;
        }

        public static void SetAutoFire(bool value)
        {
            EnsureLoaded();
            _autoFire = value;
            PlayerPrefs.SetInt(AutoFireKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetReducedMotion(bool value)
        {
            EnsureLoaded();
            _reducedMotion = value;
            PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetHaptics(bool value)
        {
            EnsureLoaded();
            _haptics = value;
            PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetColorblindSafeTelegraphs(bool value)
        {
            EnsureLoaded();
            _colorblindSafeTelegraphs = value;
            PlayerPrefs.SetInt(ColorblindSafeTelegraphsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Writes every current value to <see cref="PlayerPrefs"/>.
        /// </summary>
        public static void Save()
        {
            EnsureLoaded();
            PersistValues();
        }

        /// <summary>
        /// Reloads every value from <see cref="PlayerPrefs"/> and clamps it.
        /// </summary>
        public static void Load()
        {
            _loaded = false;
            EnsureLoaded();
        }

        /// <summary>
        /// Restores and persists the documented defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            EnsureLoaded();
            _leftHanded = DefaultLeftHanded;
            _joystickSize = DefaultJoystickSize;
            _joystickOpacity = DefaultJoystickOpacity;
            _aimAssist = DefaultAimAssist;
            _autoFire = DefaultAutoFire;
            _reducedMotion = DefaultReducedMotion;
            _haptics = DefaultHaptics;
            _colorblindSafeTelegraphs = DefaultColorblindSafeTelegraphs;
            PersistValues();
        }

        public static float ClampJoystickSize(float value)
        {
            return Snap(value, MinJoystickSize, MaxJoystickSize, JoystickSizeStep);
        }

        public static float ClampJoystickOpacity(float value)
        {
            return Snap(value, MinJoystickOpacity, MaxJoystickOpacity, JoystickOpacityStep);
        }

        /// <summary>
        /// Test hook that clears the device-local keys and reloads the defaults.
        /// </summary>
        public static void ClearPreferencesForTests()
        {
            PlayerPrefs.DeleteKey(LeftHandedKey);
            PlayerPrefs.DeleteKey(JoystickSizeKey);
            PlayerPrefs.DeleteKey(JoystickOpacityKey);
            PlayerPrefs.DeleteKey(AimAssistKey);
            PlayerPrefs.DeleteKey(AutoFireKey);
            PlayerPrefs.DeleteKey(ReducedMotionKey);
            PlayerPrefs.DeleteKey(HapticsKey);
            PlayerPrefs.DeleteKey(ColorblindSafeTelegraphsKey);
            PlayerPrefs.Save();
            Load();
        }

        /// <summary>
        /// Test hook that reloads values after a test writes raw preferences.
        /// </summary>
        public static void ReloadForTests()
        {
            Load();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            var needsSave = false;
            _leftHanded = PlayerPrefs.GetInt(LeftHandedKey, DefaultLeftHanded ? 1 : 0) != 0;
            _autoFire = PlayerPrefs.GetInt(AutoFireKey, DefaultAutoFire ? 1 : 0) != 0;
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, DefaultReducedMotion ? 1 : 0) != 0;
            _haptics = PlayerPrefs.GetInt(HapticsKey, DefaultHaptics ? 1 : 0) != 0;
            _colorblindSafeTelegraphs = PlayerPrefs.GetInt(
                ColorblindSafeTelegraphsKey,
                DefaultColorblindSafeTelegraphs ? 1 : 0) != 0;

            var rawSize = PlayerPrefs.GetFloat(JoystickSizeKey, DefaultJoystickSize);
            _joystickSize = ClampJoystickSize(rawSize);
            if (!Mathf.Approximately(rawSize, _joystickSize))
            {
                needsSave = true;
            }

            var rawOpacity = PlayerPrefs.GetFloat(JoystickOpacityKey, DefaultJoystickOpacity);
            _joystickOpacity = ClampJoystickOpacity(rawOpacity);
            if (!Mathf.Approximately(rawOpacity, _joystickOpacity))
            {
                needsSave = true;
            }

            var rawAimAssist = PlayerPrefs.GetInt(AimAssistKey, (int)DefaultAimAssist);
            if (rawAimAssist < (int)AimAssistLevel.Off || rawAimAssist > (int)AimAssistLevel.High)
            {
                _aimAssist = DefaultAimAssist;
                needsSave = true;
            }
            else
            {
                _aimAssist = (AimAssistLevel)rawAimAssist;
            }

            if (needsSave)
            {
                PersistValues();
            }
        }

        private static void PersistValues()
        {
            PlayerPrefs.SetInt(LeftHandedKey, _leftHanded ? 1 : 0);
            PlayerPrefs.SetFloat(JoystickSizeKey, _joystickSize);
            PlayerPrefs.SetFloat(JoystickOpacityKey, _joystickOpacity);
            PlayerPrefs.SetInt(AimAssistKey, (int)_aimAssist);
            PlayerPrefs.SetInt(AutoFireKey, _autoFire ? 1 : 0);
            PlayerPrefs.SetInt(ReducedMotionKey, _reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(HapticsKey, _haptics ? 1 : 0);
            PlayerPrefs.SetInt(ColorblindSafeTelegraphsKey, _colorblindSafeTelegraphs ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static bool IsValidAimAssist(AimAssistLevel value)
        {
            return value == AimAssistLevel.Off ||
                   value == AimAssistLevel.Low ||
                   value == AimAssistLevel.High;
        }

        private static float Snap(float value, float min, float max, float step)
        {
            var clamped = Mathf.Clamp(value, min, max);
            var steps = Mathf.Round((clamped - min) / step);
            var snapped = min + (steps * step);
            return Mathf.Round(Mathf.Clamp(snapped, min, max) * 10f) / 10f;
        }
    }
}
