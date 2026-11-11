using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            PlayerPrefs.DeleteKey(GameAudio.MutedKey);
            PlayerPrefs.Save();

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
            PlayerPrefs.DeleteKey(GameAudio.MutedKey);
            PlayerPrefs.Save();

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
        public IEnumerator Pause_menu_sound_label_toggles_between_on_and_off()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var session = _bootstrap.Session;
            session.StartRun();
            session.PauseGame();
            yield return null;

            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: ON"));

            session.ToggleSoundForTests();
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: OFF"));
            Assert.That(session.AudioForTests.IsMuted, Is.True);

            session.ToggleSoundForTests();
            Assert.That(session.SoundLabelForTests, Is.EqualTo("SOUND: ON"));
            Assert.That(session.AudioForTests.IsMuted, Is.False);
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
    }
}
