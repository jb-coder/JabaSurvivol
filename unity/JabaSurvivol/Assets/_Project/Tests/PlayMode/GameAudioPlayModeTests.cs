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

            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-audio", Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;

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
        public IEnumerator Pause_menu_sound_label_toggles_between_on_and_off()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            session.PauseGame();
            yield return null;

            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: ON"));

            session.SoundButtonForTests.onClick.Invoke();
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: OFF"));
            Assert.That(session.AudioForTests.IsMuted, Is.True);

            session.SoundButtonForTests.onClick.Invoke();
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: ON"));
            Assert.That(session.AudioForTests.IsMuted, Is.False);
        }

        [UnityTest]
        public IEnumerator Main_menu_audio_panel_toggles_mute_and_volume()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.AudioButtonMainForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.AudioPanelVisibleForTests, Is.True);
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: ON"));
            Assert.That(session.AudioMasterLabelForTests, Is.EqualTo("VOL 100%"));
            Assert.That(session.AudioEffectsLabelForTests, Is.EqualTo("FX 80%"));

            session.AudioMuteButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.IsMuted, Is.True);
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: OFF"));

            session.AudioMuteButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.IsMuted, Is.False);

            session.AudioMasterMinusButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.MasterVolume, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(session.AudioMasterLabelForTests, Is.EqualTo("VOL 90%"));

            session.AudioEffectsMinusButtonForTests.onClick.Invoke();
            Assert.That(session.AudioForTests.EffectsVolume, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(session.AudioEffectsLabelForTests, Is.EqualTo("FX 70%"));

            session.AudioCloseButtonForTests.onClick.Invoke();
            Assert.That(session.AudioPanelVisibleForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator Session_pauses_and_resumes_audio()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            Assert.That(session.AudioForTests.IsSuspended, Is.False);

            session.PauseGame();
            Assert.That(session.AudioForTests.IsSuspended, Is.True);

            session.ResumeGame();
            Assert.That(session.AudioForTests.IsSuspended, Is.False);

            session.SetApplicationPausedForTests(true);
            Assert.That(session.AudioForTests.IsSuspended, Is.True);

            session.SetApplicationPausedForTests(false);
            Assert.That(session.AudioForTests.IsSuspended, Is.False);

            session.SetApplicationFocusedForTests(false);
            Assert.That(session.AudioForTests.IsSuspended, Is.True);

            session.SetApplicationFocusedForTests(true);
            Assert.That(session.AudioForTests.IsSuspended, Is.False);
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
        public IEnumerator Ui_button_click_emits_a_cue()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.ResetAudioHistoryForTests();
            session.LanguageButtonForTests.onClick.Invoke();

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
