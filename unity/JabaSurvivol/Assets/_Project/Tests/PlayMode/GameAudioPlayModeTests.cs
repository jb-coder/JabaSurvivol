using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class GameAudioPlayModeTests
    {
        private readonly List<GameObject> _created = new();
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            DeleteAudioPreferences();
            GameSettings.ClearPreferencesForTests();

            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-audio", Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;
            GameSettings.ClearPreferencesForTests();

            if (_bootstrap != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            foreach (var gameObject in _created)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }

            _created.Clear();
            DeleteAudioPreferences();

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Mute_preference_persists_across_instances()
        {
            var first = CreateAudio();
            yield return null;

            Assert.That(first.IsMuted, Is.False);
            first.ToggleMute();
            Assert.That(first.IsMuted, Is.True);

            var firstObject = first.gameObject;
            UnityEngine.Object.DestroyImmediate(firstObject);
            _created.Remove(firstObject);
            yield return null;

            var second = CreateAudio();
            yield return null;

            Assert.That(second.IsMuted, Is.True);
            Assert.That(second.PlayedCueCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Play_is_silent_and_uncounted_while_muted()
        {
            var audio = CreateAudio();
            yield return null;

            audio.SetMuted(true);
            audio.Play(GameAudioCue.Kill);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(0));

            audio.SetMuted(false);
            audio.Play(GameAudioCue.Kill);

            Assert.That(audio.PlayedCueCount, Is.EqualTo(1));
            Assert.That(audio.LastCue, Is.EqualTo(GameAudioCue.Kill));
        }

        [UnityTest]
        public IEnumerator Every_cue_has_a_generated_clip()
        {
            var audio = CreateAudio();
            yield return null;

            var cues = (GameAudioCue[])Enum.GetValues(typeof(GameAudioCue));
            Assert.That(cues.Length, Is.EqualTo(audio.CueCountForTests));

            foreach (var cue in cues)
            {
                Assert.That(audio.HasClipForTests(cue), Is.True, cue.ToString());
                Assert.That(audio.ClipSampleCountForTests(cue), Is.GreaterThan(0), cue.ToString());
            }
        }

        [UnityTest]
        public IEnumerator Volume_clamps_and_persists()
        {
            var first = CreateAudio();
            yield return null;

            first.SetMasterVolume(1.5f);
            first.SetEffectsVolume(-0.5f);

            Assert.That(first.MasterVolume, Is.EqualTo(1f));
            Assert.That(first.EffectsVolume, Is.EqualTo(0f));

            var firstObject = first.gameObject;
            UnityEngine.Object.DestroyImmediate(firstObject);
            _created.Remove(firstObject);
            yield return null;

            var second = CreateAudio();
            yield return null;

            Assert.That(second.MasterVolume, Is.EqualTo(1f));
            Assert.That(second.EffectsVolume, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator Zero_volume_blocks_cues()
        {
            var audio = CreateAudio();
            yield return null;

            audio.SetMasterVolume(0f);
            audio.Play(GameAudioCue.Kill);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(0));

            audio.SetMasterVolume(1f);
            audio.SetEffectsVolume(0f);
            audio.Play(GameAudioCue.Hit);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(0));

            audio.SetEffectsVolume(1f);
            audio.Play(GameAudioCue.LevelUp);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(1));
            Assert.That(audio.LastCue, Is.EqualTo(GameAudioCue.LevelUp));
        }

        [UnityTest]
        public IEnumerator Suspend_blocks_cues_and_resumes()
        {
            var audio = CreateAudio();
            yield return null;

            audio.SetSuspended(true);
            audio.Play(GameAudioCue.Kill);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(0));

            audio.SetSuspended(false);
            audio.Play(GameAudioCue.Kill);
            Assert.That(audio.PlayedCueCount, Is.EqualTo(1));
            Assert.That(audio.LastCue, Is.EqualTo(GameAudioCue.Kill));
        }

        [UnityTest]
        public IEnumerator Voice_limit_bounds_simultaneous_cues()
        {
            var audio = CreateAudio();
            yield return null;

            foreach (GameAudioCue cue in Enum.GetValues(typeof(GameAudioCue)))
            {
                audio.Play(cue);
            }

            Assert.That(audio.PlayedCueCount, Is.LessThanOrEqualTo(GameAudio.VoiceCount));
            Assert.That(audio.ActiveVoiceCountForTests, Is.LessThanOrEqualTo(GameAudio.VoiceCount));
        }

        [UnityTest]
        public IEnumerator Settings_panel_sound_label_toggles_between_on_and_off()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            session.PauseGame();
            yield return null;

            session.SettingsButtonPauseForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.SettingsPanelVisibleForTests, Is.True);
            Assert.That(session.SettingsMuteLabelForTests, Is.EqualTo("ON"));

            session.SettingsMuteButtonForTests.onClick.Invoke();
            Assert.That(session.SettingsMuteLabelForTests, Is.EqualTo("OFF"));
            Assert.That(session.AudioForTests.IsMuted, Is.True);

            session.SettingsMuteButtonForTests.onClick.Invoke();
            Assert.That(session.SettingsMuteLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.AudioForTests.IsMuted, Is.False);
        }

        [UnityTest]
        public IEnumerator Settings_panel_toggles_mute_and_volume()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.ResetAudioHistoryForTests();
            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.SettingsPanelVisibleForTests, Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.UiOpen), Is.True);
            Assert.That(session.SettingsMuteLabelForTests, Is.EqualTo("ON"));
            Assert.That(session.SettingsMasterLabelForTests, Is.EqualTo("100%"));
            Assert.That(session.SettingsEffectsLabelForTests, Is.EqualTo("80%"));

            session.SettingsMuteButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.IsMuted, Is.True);
            Assert.That(session.SettingsMuteLabelForTests, Is.EqualTo("OFF"));

            session.SettingsMuteButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.IsMuted, Is.False);

            session.SettingsMasterMinusButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.MasterVolume, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(session.SettingsMasterLabelForTests, Is.EqualTo("90%"));

            session.SettingsEffectsMinusButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.EffectsVolume, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(session.SettingsEffectsLabelForTests, Is.EqualTo("70%"));

            session.ResetAudioHistoryForTests();
            session.SettingsCloseButtonForTests.onClick.Invoke();
            Assert.That(session.SettingsPanelVisibleForTests, Is.False);
            Assert.That(ContainsRecentCue(session, GameAudioCue.UiClose), Is.True);
        }

        [UnityTest]
        public IEnumerator Session_pauses_and_resumes_audio()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            var audio = session.AudioForTests;
            audio.SetMasterVolume(0.7f);
            audio.SetEffectsVolume(0.6f);
            audio.SetMuted(false);
            session.StartRun();
            yield return null;

            Assert.That(audio.IsSuspended, Is.False);
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(PlayerPrefs.GetFloat(GameAudio.MasterVolumeKey), Is.EqualTo(0.7f));
            Assert.That(PlayerPrefs.GetFloat(GameAudio.EffectsVolumeKey), Is.EqualTo(0.6f));
            Assert.That(PlayerPrefs.GetInt(GameAudio.MutedKey), Is.EqualTo(0));

            audio.Play(GameAudioCue.BossTelegraph);
            Assert.That(audio.ActiveVoiceCountForTests, Is.GreaterThan(0));

            session.PauseGame();
            Assert.That(audio.IsSuspended, Is.True);
            Assert.That(audio.ActiveVoiceCountForTests, Is.EqualTo(0));
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(audio.MasterVolume, Is.EqualTo(0.7f));
            Assert.That(audio.EffectsVolume, Is.EqualTo(0.6f));

            session.ResumeGame();
            Assert.That(audio.IsSuspended, Is.False);
            Assert.That(audio.MasterVolume, Is.EqualTo(0.7f));
            Assert.That(audio.EffectsVolume, Is.EqualTo(0.6f));

            session.SetApplicationPausedForTests(true);
            Assert.That(audio.IsSuspended, Is.True);
            session.SetApplicationPausedForTests(false);
            Assert.That(audio.IsSuspended, Is.False);

            session.SetApplicationFocusedForTests(false);
            Assert.That(audio.IsSuspended, Is.True);
            session.SetApplicationFocusedForTests(true);
            Assert.That(audio.IsSuspended, Is.False);

            audio.SetMuted(true);
            session.PauseGame();
            session.ResumeGame();
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(PlayerPrefs.GetInt(GameAudio.MutedKey), Is.EqualTo(1));
            audio.SetMuted(false);

            audio.Play(GameAudioCue.BossTelegraph);
            session.ReturnToMenu();
            Assert.That(audio.ActiveVoiceCountForTests, Is.EqualTo(0));
            Assert.That(audio.MasterVolume, Is.EqualTo(0.7f));
            Assert.That(audio.EffectsVolume, Is.EqualTo(0.6f));
        }

        [UnityTest]
        public IEnumerator Weapon_fire_reload_and_switch_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.Profile.UnlockWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;
            session.ResetAudioHistoryForTests();

            session.FireForTests();
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.PistolFire));

            Assert.That(session.ReloadWeapon(), Is.True);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.ReloadStart));

            yield return new WaitForSeconds(session.CurrentReloadDuration + 0.2f);
            Assert.That(ContainsRecentCue(session, GameAudioCue.ReloadComplete), Is.True);

            Assert.That(session.SwitchWeapon(), Is.True);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.WeaponSwitch));
        }

        [UnityTest]
        public IEnumerator Skill_and_round_events_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Dash, 1);
            session.Profile.SetActiveSkill(SkillNode.Dash);
            session.ResetAudioHistoryForTests();
            session.StartRun();
            yield return null;

            Assert.That(ContainsRecentCue(session, GameAudioCue.RoundStart), Is.True);

            session.ResetAudioHistoryForTests();
            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.Dash));

            session.ResetAudioHistoryForTests();
            yield return ClearRegularEnemies(session);
            Assert.That(ContainsRecentCue(session, GameAudioCue.RoundClear), Is.True);
        }

        [UnityTest]
        public IEnumerator Boss_events_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);

            session.ResetAudioHistoryForTests();
            session.SpawnBossForTests();
            yield return null;
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossSpawn), Is.True);

            session.ResetAudioHistoryForTests();
            session.DamageBossForTests(10f);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Impact), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossHit), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Hit), Is.False);

            session.ResetAudioHistoryForTests();
            session.SetBossAttackCooldownForTests(0f);
            yield return null;
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossTelegraph), Is.True);

            session.ResetAudioHistoryForTests();
            yield return new WaitForSeconds(BossCatalog.BossDefinition.SlamTelegraphSeconds + 0.2f);
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossSlam), Is.True);

            session.ResetAudioHistoryForTests();
            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth * 0.6f);
            yield return null;
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossEnrage), Is.True);

            session.ResetAudioHistoryForTests();
            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth);
            yield return null;
            Assert.That(ContainsRecentCue(session, GameAudioCue.BossDeath), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Victory), Is.True);
        }

        [UnityTest]
        public IEnumerator Every_weapon_fires_its_own_cue()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.Profile.UnlockWeapon(WeaponId.Smg);
            session.Profile.UnlockWeapon(WeaponId.Shotgun);
            session.Profile.UnlockWeapon(WeaponId.Rifle);
            session.Profile.UnlockWeapon(WeaponId.Lmg);
            session.StartRun();
            yield return null;

            for (var i = 0; i < 5; i++)
            {
                if (i > 0)
                {
                    Assert.That(session.SwitchWeapon(), Is.True);
                    yield return null;
                }

                session.ResetAudioHistoryForTests();
                session.FireForTests();

                var expected = ExpectedFireCue(session.CurrentWeapon);
                Assert.That(RecentCue(session, 0), Is.EqualTo(expected), session.CurrentWeapon.ToString());

                if (session.CurrentWeapon == WeaponId.Shotgun)
                {
                    Assert.That(CountRecentCue(session, GameAudioCue.ShotgunFire), Is.EqualTo(1));
                }
            }
        }

        [UnityTest]
        public IEnumerator Empty_magazine_emits_a_cue()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.Profile.UnlockWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;
            Assert.That(session.SwitchWeapon(), Is.True);

            var state = session.Run.CurrentWeaponState;
            while (state.Magazine > 0)
            {
                state.TryFire();
            }

            while (state.Reserve > 0)
            {
                Assert.That(state.StartReload(), Is.True);
                state.Tick(1000f);
                while (state.Magazine > 0)
                {
                    state.TryFire();
                }
            }

            Assert.That(state.Magazine, Is.EqualTo(0));
            Assert.That(state.Reserve, Is.EqualTo(0));

            session.ResetAudioHistoryForTests();
            Assert.That(session.ReloadWeapon(), Is.False);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.Empty));
        }

        [UnityTest]
        public IEnumerator Player_damage_zombie_impact_and_kill_emit_distinct_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            session.ResetAudioHistoryForTests();
            session.ApplyDamage(1f);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.Hit));

            session.ResetAudioHistoryForTests();
            session.DamageZombieForTests(1f);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.Impact));
            Assert.That(ContainsRecentCue(session, GameAudioCue.Hit), Is.False);

            session.ResetAudioHistoryForTests();
            session.DamageZombieForTests(session.ZombieHealthForTests);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Impact), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Kill), Is.True);
        }

        [UnityTest]
        public IEnumerator Turret_and_shockwave_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 5);
            session.Profile.SetActiveSkill(SkillNode.Turret);
            session.StartRun();
            yield return null;

            session.ResetAudioHistoryForTests();
            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.TurretDeploy));

            session.SetZombiePositionForTests(new Vector2(2f, 0f));
            session.ResetAudioHistoryForTests();
            for (var i = 0; i < 240 && session.TurretShotsFiredForTests == 0; i++)
            {
                yield return null;
            }

            Assert.That(ContainsRecentCue(session, GameAudioCue.TurretFire), Is.True);

            session.Profile.SetSkillRank(SkillNode.Shockwave, 1);
            session.Profile.SetActiveSkill(SkillNode.Shockwave);
            session.StartRun();
            yield return null;
            session.SetZombiePositionForTests(new Vector2(1f, 0f));
            session.ResetAudioHistoryForTests();

            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            Assert.That(RecentCue(session, 0), Is.EqualTo(GameAudioCue.Shockwave));
        }

        [UnityTest]
        public IEnumerator Defeat_and_reward_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            session.ResetAudioHistoryForTests();
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            Assert.That(ContainsRecentCue(session, GameAudioCue.Defeat), Is.True);
        }

        [UnityTest]
        public IEnumerator Reward_selection_emits_a_cue()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            yield return ClearRegularEnemies(session);

            session.ResetAudioHistoryForTests();
            Assert.That(session.SelectRoundReward(0), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Reward), Is.True);
        }

        [UnityTest]
        public IEnumerator Armory_purchase_and_error_emit_cues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.ShowArmory();
            yield return null;

            session.ResetAudioHistoryForTests();
            Assert.That(session.ClickArmoryWeaponActionForTests(WeaponId.Smg), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Error), Is.True);

            session.Profile.AddSupplies(10000);
            session.ResetAudioHistoryForTests();
            Assert.That(session.ClickArmoryWeaponActionForTests(WeaponId.Smg), Is.True);
            Assert.That(ContainsRecentCue(session, GameAudioCue.Purchase), Is.True);
        }

        [UnityTest]
        public IEnumerator Stop_all_releases_sources()
        {
            var audio = CreateAudio();
            yield return null;

            audio.Play(GameAudioCue.BossTelegraph);
            audio.Play(GameAudioCue.BossSlam);
            Assert.That(audio.ActiveVoiceCountForTests, Is.GreaterThan(0));

            audio.StopAll();
            yield return null;
            Assert.That(audio.ActiveVoiceCountForTests, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Ui_button_click_emits_a_cue()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.ResetAudioHistoryForTests();
            session.SettingsButtonMainForTests.onClick.Invoke();

            Assert.That(ContainsRecentCue(session, GameAudioCue.UiClick), Is.True);
        }

        private GameAudio CreateAudio()
        {
            var gameObject = new GameObject("Audio Test");
            _created.Add(gameObject);
            return gameObject.AddComponent<GameAudio>();
        }

        private GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            _created.Add(gameObject);
            return gameObject.AddComponent<GameBootstrap>();
        }

        private static void DeleteAudioPreferences()
        {
            PlayerPrefs.DeleteKey(GameAudio.MutedKey);
            PlayerPrefs.DeleteKey(GameAudio.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameAudio.EffectsVolumeKey);
            PlayerPrefs.Save();
        }

        private static GameAudioCue RecentCue(GameSession session, int offset)
        {
            return session.AudioForTests.RecentCueForTests(offset);
        }

        private static bool ContainsRecentCue(GameSession session, GameAudioCue cue)
        {
            var audio = session.AudioForTests;
            for (var i = 0; i < audio.RecentCueCountForTests && i < 8; i++)
            {
                if (audio.RecentCueForTests(i) == cue)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameAudioCue ExpectedFireCue(WeaponId weaponId)
        {
            switch (weaponId)
            {
                case WeaponId.Smg:
                    return GameAudioCue.SmgFire;
                case WeaponId.Shotgun:
                    return GameAudioCue.ShotgunFire;
                case WeaponId.Rifle:
                    return GameAudioCue.RifleFire;
                case WeaponId.Lmg:
                    return GameAudioCue.LmgFire;
                default:
                    return GameAudioCue.PistolFire;
            }
        }

        private static int CountRecentCue(GameSession session, GameAudioCue cue)
        {
            var audio = session.AudioForTests;
            var count = 0;
            for (var i = 0; i < audio.RecentCueCountForTests && i < 8; i++)
            {
                if (audio.RecentCueForTests(i) == cue)
                {
                    count++;
                }
            }

            return count;
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
    }
}
