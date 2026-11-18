using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class SettingsPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "jabasurvivol-settings",
                System.Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            DeleteAudioPreferences();
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            DeleteAudioPreferences();
            GameBootstrap.SavePathOverride = null;

            if (_bootstrap != null)
            {
                Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Main_menu_and_pause_open_the_same_settings_panel()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.SettingsPanelVisibleForTests, Is.True);
            Assert.That(session.SettingsButtonMainForTests, Is.Not.Null);
            Assert.That(session.SettingsButtonPauseForTests, Is.Not.Null);

            session.SettingsCloseButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.SettingsPanelVisibleForTests, Is.False);

            session.StartRun();
            yield return null;
            session.PauseGame();
            yield return null;

            session.SettingsButtonPauseForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.SettingsPanelVisibleForTests, Is.True);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));
        }

        [UnityTest]
        public IEnumerator Control_and_accessibility_settings_persist_across_bootstrap_recreation()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;

            session.SettingsLeftHandedButtonForTests.onClick.Invoke();
            session.SettingsJoystickSizePlusButtonForTests.onClick.Invoke();
            session.SettingsJoystickSizePlusButtonForTests.onClick.Invoke();
            session.SettingsJoystickOpacityMinusButtonForTests.onClick.Invoke();
            session.SettingsAimAssistButtonForTests.onClick.Invoke();
            session.SettingsAutoFireButtonForTests.onClick.Invoke();
            session.SettingsReducedMotionButtonForTests.onClick.Invoke();
            session.SettingsHapticsButtonForTests.onClick.Invoke();
            session.SettingsColorblindButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(GameSettings.LeftHanded, Is.True);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Low));
            Assert.That(GameSettings.AutoFire, Is.False);
            Assert.That(GameSettings.ReducedMotion, Is.True);
            Assert.That(GameSettings.Haptics, Is.True);
            Assert.That(GameSettings.ColorblindSafeTelegraphs, Is.True);

            Assert.That(session.SettingsLeftHandedLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsJoystickSizeLabelForTests, Is.EqualTo("1.2x"));
            Assert.That(session.SettingsJoystickOpacityLabelForTests, Is.EqualTo("90%"));
            Assert.That(session.SettingsAimAssistLabelForTests, Is.EqualTo("LOW"));
            Assert.That(session.SettingsAutoFireLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsReducedMotionLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsHapticsLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsColorblindLabelForTests, Is.EqualTo("ON"));

            Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            GameSettings.ReloadForTests();
            _bootstrap = CreateBootstrap();
            yield return null;
            session = _bootstrap.Session;

            Assert.That(session.SettingsLeftHandedLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsJoystickSizeLabelForTests, Is.EqualTo("1.2x"));
            Assert.That(session.SettingsJoystickOpacityLabelForTests, Is.EqualTo("90%"));
            Assert.That(session.SettingsAimAssistLabelForTests, Is.EqualTo("LOW"));
            Assert.That(session.SettingsAutoFireLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsReducedMotionLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsHapticsLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsColorblindLabelForTests, Is.EqualTo("ON"));
        }

        [UnityTest]
        public IEnumerator Reset_restores_control_and_accessibility_defaults()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;

            session.SettingsLeftHandedButtonForTests.onClick.Invoke();
            session.SettingsJoystickSizeMinusButtonForTests.onClick.Invoke();
            session.SettingsJoystickOpacityPlusButtonForTests.onClick.Invoke();
            session.SettingsAimAssistButtonForTests.onClick.Invoke();
            session.SettingsAutoFireButtonForTests.onClick.Invoke();
            session.SettingsReducedMotionButtonForTests.onClick.Invoke();
            session.SettingsHapticsButtonForTests.onClick.Invoke();
            session.SettingsColorblindButtonForTests.onClick.Invoke();

            session.SettingsResetButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(GameSettings.LeftHanded, Is.False);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(GameSettings.AutoFire, Is.True);
            Assert.That(GameSettings.ReducedMotion, Is.False);
            Assert.That(GameSettings.Haptics, Is.False);
            Assert.That(GameSettings.ColorblindSafeTelegraphs, Is.False);

            Assert.That(session.SettingsLeftHandedLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsJoystickSizeLabelForTests, Is.EqualTo("1.0x"));
            Assert.That(session.SettingsJoystickOpacityLabelForTests, Is.EqualTo("100%"));
            Assert.That(session.SettingsAimAssistLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsAutoFireLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsReducedMotionLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsHapticsLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.SettingsColorblindLabelForTests, Is.EqualTo("OFF"));

            Assert.That(PlayerPrefs.GetInt(GameSettings.LeftHandedKey), Is.EqualTo(0));
            Assert.That(
                PlayerPrefs.GetFloat(GameSettings.JoystickSizeKey),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(PlayerPrefs.GetInt(GameSettings.AimAssistKey), Is.EqualTo(0));
            Assert.That(PlayerPrefs.GetInt(GameSettings.AutoFireKey), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Reset_keeps_audio_and_language_preferences()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;

            session.SettingsMuteButtonForTests.onClick.Invoke();
            session.SettingsMasterMinusButtonForTests.onClick.Invoke();
            session.SettingsLanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;

            var masterVolume = session.AudioForTests.MasterVolume;
            Assert.That(session.AudioForTests.IsMuted, Is.True);
            Assert.That(masterVolume, Is.EqualTo(0.9f).Within(0.001f));

            session.SettingsResetButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.AudioForTests.IsMuted, Is.True);
            Assert.That(session.AudioForTests.MasterVolume, Is.EqualTo(masterVolume).Within(0.001f));
            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
            Assert.That(session.SettingsLanguageCurrentLabelForTests, Is.EqualTo("Español"));
        }

        private GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Settings Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }

        private static void DeleteAudioPreferences()
        {
            PlayerPrefs.DeleteKey(GameAudio.MutedKey);
            PlayerPrefs.DeleteKey(GameAudio.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameAudio.EffectsVolumeKey);
            PlayerPrefs.Save();
        }
    }
}
