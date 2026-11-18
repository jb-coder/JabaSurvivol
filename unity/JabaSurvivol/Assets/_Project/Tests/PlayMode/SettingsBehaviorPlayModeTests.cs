using System;
using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class SettingsBehaviorPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "jabasurvivol-settings-behavior",
                Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            GameSettings.MarkHelpSeen();
            HapticFeedback.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            HapticFeedback.ResetForTests();
            GameBootstrap.SavePathOverride = null;

            if (_bootstrap != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Default_hud_layout_places_move_left_and_aim_right()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            session.StartRun();
            yield return null;

            Assert.That(session.MoveStickAnchorForTests, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(session.AimStickAnchorForTests, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(session.MoveStickSizeForTests, Is.EqualTo(260f).Within(0.01f));
            Assert.That(session.AimStickSizeForTests, Is.EqualTo(260f).Within(0.01f));
            Assert.That(session.MoveKnobSizeForTests, Is.EqualTo(96f).Within(0.01f));
            Assert.That(session.AimKnobSizeForTests, Is.EqualTo(96f).Within(0.01f));
            Assert.That(session.MoveBaseColorForTests.a, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(session.MoveKnobColorForTests.a, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(session.ReloadButtonPositionForTests.x, Is.EqualTo(-110f).Within(0.01f));
            Assert.That(session.SwitchWeaponButtonPositionForTests.x, Is.EqualTo(100f).Within(0.01f));
            Assert.That(session.PauseButtonPositionForTests.x, Is.EqualTo(-30f).Within(0.01f));
            Assert.That(session.ActiveSkillButtonPositionForTests.x, Is.EqualTo(-170f).Within(0.01f));
            Assert.That(session.FireButtonVisibleForTests, Is.False);
            Assert.That(
                session.ControlsHintForTests,
                Is.EqualTo("LEFT STICK MOVE    RIGHT STICK AIM & FIRE"));
        }

        [UnityTest]
        public IEnumerator Left_handed_setting_mirrors_sticks_actions_and_hint()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;

            GameSettings.SetLeftHanded(true);
            session.StartRun();
            yield return null;

            Assert.That(session.MoveStickAnchorForTests, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(session.AimStickAnchorForTests, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(session.ReloadButtonPositionForTests.x, Is.EqualTo(110f).Within(0.01f));
            Assert.That(session.SwitchWeaponButtonPositionForTests.x, Is.EqualTo(-100f).Within(0.01f));
            Assert.That(session.PauseButtonPositionForTests.x, Is.EqualTo(30f).Within(0.01f));
            Assert.That(session.ActiveSkillButtonPositionForTests.x, Is.EqualTo(170f).Within(0.01f));
            Assert.That(
                session.ControlsHintForTests,
                Is.EqualTo("RIGHT STICK MOVE    LEFT STICK AIM & FIRE"));
        }

        [UnityTest]
        public IEnumerator Joystick_size_and_opacity_apply_together_without_changing_hit_targets()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            session.StartRun();
            yield return null;

            GameSettings.SetJoystickSize(1.4f);
            GameSettings.SetJoystickOpacity(0.5f);
            session.ApplyControlLayoutForTests();
            yield return null;

            Assert.That(session.MoveStickSizeForTests, Is.EqualTo(364f).Within(0.01f));
            Assert.That(session.AimStickSizeForTests, Is.EqualTo(364f).Within(0.01f));
            Assert.That(session.MoveKnobSizeForTests, Is.EqualTo(134.4f).Within(0.01f));
            Assert.That(session.AimKnobSizeForTests, Is.EqualTo(134.4f).Within(0.01f));
            Assert.That(session.MoveBaseColorForTests.a, Is.EqualTo(0.075f).Within(0.001f));
            Assert.That(session.AimBaseColorForTests.a, Is.EqualTo(0.075f).Within(0.001f));
            Assert.That(session.MoveKnobColorForTests.a, Is.EqualTo(0.21f).Within(0.001f));
            Assert.That(session.AimKnobColorForTests.a, Is.EqualTo(0.21f).Within(0.001f));

            // The rest position stays inside the safe area at maximum size.
            var half = session.MoveStickSizeForTests * 0.5f;
            Assert.That(session.MoveStickPositionForTests.x - half, Is.GreaterThanOrEqualTo(0f));
            Assert.That(session.MoveStickPositionForTests.y - half, Is.GreaterThanOrEqualTo(0f));
            Assert.That(session.AimStickPositionForTests.x + half, Is.LessThanOrEqualTo(0f));

            // Opacity changes visuals only; the scaled hit target stays put.
            GameSettings.SetJoystickOpacity(1f);
            session.ApplyControlLayoutForTests();
            yield return null;
            Assert.That(session.MoveStickSizeForTests, Is.EqualTo(364f).Within(0.01f));
            Assert.That(session.MoveKnobSizeForTests, Is.EqualTo(134.4f).Within(0.01f));
            Assert.That(session.MoveBaseColorForTests.a, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(session.MoveKnobColorForTests.a, Is.EqualTo(0.42f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Maximum_joystick_size_keeps_the_active_skill_button_clear()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            session.StartRun();
            yield return null;

            GameSettings.SetJoystickSize(1.4f);
            session.ApplyControlLayoutForTests();
            yield return null;

            Assert.That(
                session.ActiveSkillButtonPositionForTests.y,
                Is.GreaterThanOrEqualTo(session.AimStickSizeForTests + 20f));

            GameSettings.SetLeftHanded(true);
            session.ApplyControlLayoutForTests();
            yield return null;

            Assert.That(
                session.ActiveSkillButtonPositionForTests.y,
                Is.GreaterThanOrEqualTo(session.MoveStickSizeForTests + 20f));
        }

        [UnityTest]
        public IEnumerator Aim_assist_off_low_and_high_blend_and_fall_back_to_raw_aim()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Aim = Vector2.right }
            });
            session.StartRun();
            yield return null;

            session.SetPlayerPositionForTests(Vector2.zero);
            session.SetZombiePositionForTests(new Vector2(5f, 0.4f));
            yield return null;

            GameSettings.SetAimAssist(AimAssistLevel.Off);
            yield return null;
            Assert.That(AimAngle(session), Is.LessThan(0.1f));

            GameSettings.SetAimAssist(AimAssistLevel.Low);
            yield return null;
            var lowAngle = AimAngle(session);
            Assert.That(lowAngle, Is.GreaterThan(0.2f));
            Assert.That(lowAngle, Is.LessThan(4.6f));

            session.SetZombiePositionForTests(new Vector2(5f, 0.88f));
            GameSettings.SetAimAssist(AimAssistLevel.High);
            yield return null;
            var highAngle = AimAngle(session);
            Assert.That(highAngle, Is.GreaterThan(lowAngle));
            Assert.That(highAngle, Is.LessThan(10f));

            // Target outside the high cone falls back to raw aim.
            session.SetZombiePositionForTests(new Vector2(5f, 5f));
            yield return null;
            Assert.That(AimAngle(session), Is.LessThan(0.1f));

            // Target outside the maximum range falls back to raw aim.
            session.SetZombiePositionForTests(new Vector2(14f, 1.2f));
            yield return null;
            Assert.That(AimAngle(session), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator Auto_fire_off_aim_only_does_not_fire_until_the_fire_button_is_held()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            GameSettings.SetAutoFire(false);
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Aim = Vector2.right }
            });
            session.StartRun();
            yield return null;
            yield return null;

            Assert.That(session.FireButtonVisibleForTests, Is.True);
            Assert.That(
                session.IsFireButtonTouchForTests(session.FireButtonScreenCenterForTests),
                Is.True);
            Assert.That(session.IsFireButtonTouchForTests(Vector2.zero), Is.False);
            Assert.That(session.ActiveBullets, Is.EqualTo(0));

            session.SetFireButtonPressedForTests(true);
            yield return null;
            yield return null;
            Assert.That(session.ActiveBullets, Is.GreaterThan(0));

            session.SetFireButtonPressedForTests(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Haptics_off_makes_no_pulses_and_on_produces_pulses()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.EnemySpawningEnabledForTests = false;
            session.StartRun();
            yield return null;

            GameSettings.SetHaptics(false);
            session.ApplyDamage(1f);
            yield return null;
            Assert.That(HapticFeedback.PulseCountForTests, Is.EqualTo(0));

            GameSettings.SetHaptics(true);
            session.ApplyDamage(1f);
            yield return null;
            Assert.That(HapticFeedback.PulseCountForTests, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Reduced_motion_disables_bloom_and_vignette_immediately()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var polish = _bootstrap.VisualPolishForTests;

            Assert.That(polish.Enabled, Is.True);
            Assert.That(polish.BloomActive, Is.EqualTo(VisualPolish.PostProcessingActive));
            Assert.That(polish.VignetteActive, Is.EqualTo(VisualPolish.PostProcessingActive));

            GameSettings.SetReducedMotion(true);
            yield return null;
            Assert.That(polish.Enabled, Is.False);
            Assert.That(polish.BloomActive, Is.False);
            Assert.That(polish.VignetteActive, Is.False);

            GameSettings.SetReducedMotion(false);
            yield return null;
            Assert.That(polish.Enabled, Is.True);
            Assert.That(polish.BloomActive, Is.EqualTo(VisualPolish.PostProcessingActive));
            Assert.That(polish.VignetteActive, Is.EqualTo(VisualPolish.PostProcessingActive));
        }

        [UnityTest]
        public IEnumerator Reduced_motion_at_startup_keeps_post_processing_off()
        {
            GameSettings.SetReducedMotion(true);
            _bootstrap = CreateBootstrap();
            yield return null;

            var polish = _bootstrap.VisualPolishForTests;
            Assert.That(polish.Enabled, Is.False);
            Assert.That(polish.BloomActive, Is.False);
            Assert.That(polish.VignetteActive, Is.False);
            Assert.That(VisualPolish.PostProcessingActive, Is.False);
        }

        [UnityTest]
        public IEnumerator Colorblind_safe_telegraph_adds_an_outline_and_keeps_the_timing()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            GameSettings.SetColorblindSafeTelegraphs(false);
            session.SetBossAttackCooldownForTests(0f);
            yield return null;
            Assert.That(session.BossTelegraphing, Is.True);
            Assert.That(session.BossTelegraphOutlineVisible, Is.False);

            GameSettings.SetColorblindSafeTelegraphs(true);
            yield return null;
            Assert.That(session.BossTelegraphing, Is.True);
            Assert.That(session.BossTelegraphOutlineVisible, Is.True);

            GameSettings.SetColorblindSafeTelegraphs(false);
            yield return null;
            Assert.That(session.BossTelegraphing, Is.True);
            Assert.That(session.BossTelegraphOutlineVisible, Is.False);
        }

        private static float AimAngle(GameSession session)
        {
            var aim = session.AimDirectionForTests;
            return Mathf.Abs(Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        }

        private static IEnumerator ClearRegularEnemies(GameSession session)
        {
            var budget = session.SpawnBudget;
            for (var i = 0; i < budget; i++)
            {
                session.DamageZombieForTests(100000f);
                yield return null;

                if (i < budget - 1)
                {
                    session.SpawnZombieForTests();
                    yield return null;
                }
            }
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Settings Behavior Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }

        private sealed class ScriptedInput : IGameInput
        {
            public InputFrame Frame;

            public InputFrame Read(Vector2 playerWorldPosition)
            {
                return Frame;
            }
        }
    }
}
