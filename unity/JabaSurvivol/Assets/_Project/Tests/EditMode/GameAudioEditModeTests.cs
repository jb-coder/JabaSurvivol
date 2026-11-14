using System;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class GameAudioEditModeTests
    {
        [Test]
        public void Every_cue_has_a_clip_at_the_expected_sample_rate()
        {
            var gameObject = new GameObject("Audio EditMode Test");
            try
            {
                var audio = gameObject.AddComponent<GameAudio>();
                audio.InitializeForTests();

                var cues = (GameAudioCue[])Enum.GetValues(typeof(GameAudioCue));
                Assert.That(cues.Length, Is.EqualTo(audio.CueCountForTests));

                foreach (var cue in cues)
                {
                    Assert.That(audio.HasClipForTests(cue), Is.True, cue.ToString());
                    Assert.That(audio.ClipSampleCountForTests(cue), Is.GreaterThan(0), cue.ToString());
                    Assert.That(audio.ClipSampleRateForTests(cue), Is.EqualTo(22050), cue.ToString());
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
