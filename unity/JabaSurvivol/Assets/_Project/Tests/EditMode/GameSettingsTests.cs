using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class GameSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            GameSettings.ClearPreferencesForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.ClearPreferencesForTests();
        }

        [Test]
        public void Defaults_are_applied_when_keys_are_absent()
        {
            Assert.That(GameSettings.LeftHanded, Is.False);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(GameSettings.AutoFire, Is.True);
            Assert.That(GameSettings.ReducedMotion, Is.False);
            Assert.That(GameSettings.Haptics, Is.False);
            Assert.That(GameSettings.ColorblindSafeTelegraphs, Is.False);
        }

        [Test]
        public void Float_settings_clamp_and_snap_on_write()
        {
            GameSettings.SetJoystickSize(2f);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.MaxJoystickSize).Within(0.001f));
            Assert.That(
                PlayerPrefs.GetFloat(GameSettings.JoystickSizeKey),
                Is.EqualTo(GameSettings.MaxJoystickSize).Within(0.001f));

            GameSettings.SetJoystickSize(0.1f);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.MinJoystickSize).Within(0.001f));

            GameSettings.SetJoystickSize(1.04f);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(1f).Within(0.001f));

            GameSettings.SetJoystickOpacity(2f);
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(GameSettings.MaxJoystickOpacity).Within(0.001f));

            GameSettings.SetJoystickOpacity(0.1f);
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(GameSettings.MinJoystickOpacity).Within(0.001f));

            GameSettings.SetJoystickOpacity(0.76f);
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void Load_clamps_invalid_preferences()
        {
            PlayerPrefs.SetFloat(GameSettings.JoystickSizeKey, 99f);
            PlayerPrefs.SetFloat(GameSettings.JoystickOpacityKey, -5f);
            PlayerPrefs.SetInt(GameSettings.AimAssistKey, 42);
            PlayerPrefs.Save();

            GameSettings.ReloadForTests();

            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.MaxJoystickSize).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(GameSettings.MinJoystickOpacity).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(
                PlayerPrefs.GetFloat(GameSettings.JoystickSizeKey),
                Is.EqualTo(GameSettings.MaxJoystickSize).Within(0.001f));
        }

        [Test]
        public void Settings_persist_across_reload()
        {
            GameSettings.SetLeftHanded(true);
            GameSettings.SetJoystickSize(1.3f);
            GameSettings.SetJoystickOpacity(0.5f);
            GameSettings.SetAimAssist(AimAssistLevel.High);
            GameSettings.SetAutoFire(false);
            GameSettings.SetReducedMotion(true);
            GameSettings.SetHaptics(true);
            GameSettings.SetColorblindSafeTelegraphs(true);

            GameSettings.ReloadForTests();

            Assert.That(GameSettings.LeftHanded, Is.True);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.High));
            Assert.That(GameSettings.AutoFire, Is.False);
            Assert.That(GameSettings.ReducedMotion, Is.True);
            Assert.That(GameSettings.Haptics, Is.True);
            Assert.That(GameSettings.ColorblindSafeTelegraphs, Is.True);
        }

        [Test]
        public void Reset_restores_the_documented_defaults()
        {
            GameSettings.SetLeftHanded(true);
            GameSettings.SetJoystickSize(1.4f);
            GameSettings.SetJoystickOpacity(0.4f);
            GameSettings.SetAimAssist(AimAssistLevel.Low);
            GameSettings.SetAutoFire(false);
            GameSettings.SetReducedMotion(true);
            GameSettings.SetHaptics(true);
            GameSettings.SetColorblindSafeTelegraphs(true);

            GameSettings.ResetToDefaults();

            Assert.That(GameSettings.LeftHanded, Is.False);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.DefaultJoystickSize).Within(0.001f));
            Assert.That(GameSettings.JoystickOpacity, Is.EqualTo(GameSettings.DefaultJoystickOpacity).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(GameSettings.AutoFire, Is.True);
            Assert.That(GameSettings.ReducedMotion, Is.False);
            Assert.That(GameSettings.Haptics, Is.False);
            Assert.That(GameSettings.ColorblindSafeTelegraphs, Is.False);

            GameSettings.ReloadForTests();

            Assert.That(GameSettings.LeftHanded, Is.False);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.DefaultJoystickSize).Within(0.001f));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(GameSettings.AutoFire, Is.True);
        }

        [Test]
        public void Aim_assist_cycles_off_low_high_and_back()
        {
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));

            Assert.That(GameSettings.CycleAimAssist(), Is.EqualTo(AimAssistLevel.Low));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Low));

            Assert.That(GameSettings.CycleAimAssist(), Is.EqualTo(AimAssistLevel.High));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.High));

            Assert.That(GameSettings.CycleAimAssist(), Is.EqualTo(AimAssistLevel.Off));
            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
            Assert.That(
                PlayerPrefs.GetInt(GameSettings.AimAssistKey),
                Is.EqualTo((int)AimAssistLevel.Off));
        }

        [Test]
        public void Invalid_aim_assist_write_falls_back_to_off()
        {
            GameSettings.SetAimAssist((AimAssistLevel)99);

            Assert.That(GameSettings.AimAssist, Is.EqualTo(AimAssistLevel.Off));
        }

        [Test]
        public void Test_clear_helper_restores_defaults()
        {
            GameSettings.SetLeftHanded(true);
            GameSettings.SetJoystickSize(1.4f);

            GameSettings.ClearPreferencesForTests();

            Assert.That(GameSettings.LeftHanded, Is.False);
            Assert.That(GameSettings.JoystickSize, Is.EqualTo(GameSettings.DefaultJoystickSize).Within(0.001f));
        }
    }
}
