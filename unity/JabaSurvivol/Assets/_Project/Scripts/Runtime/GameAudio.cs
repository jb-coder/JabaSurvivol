using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Audio cue identifiers. Every cue maps to a procedural clip generated at
    /// runtime so the project does not depend on imported audio assets.
    /// </summary>
    public enum GameAudioCue
    {
        Button,
        Kill,
        Hit,
        LevelUp,
        Reward,
        Victory,
        Defeat
    }

    /// <summary>
    /// Lightweight procedural audio feedback. Clips are generated once and
    /// cached, and the mute preference is persisted in <see cref="PlayerPrefs"/>.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public const string MutedKey = "audio.muted";

        private const int SampleRate = 22050;
        private const int CueCount = 7;
        private const float Amplitude = 0.12f;

        private readonly AudioClip[] _clips = new AudioClip[CueCount];
        private AudioSource _source;
        private bool _muted;

        /// <summary>
        /// True when cue output is muted. The preference is persisted.
        /// </summary>
        public bool IsMuted => _muted;

        /// <summary>
        /// Number of audible cues requested since this instance started.
        /// </summary>
        public int PlayedCueCount { get; private set; }

        /// <summary>
        /// The most recent cue requested while unmuted.
        /// </summary>
        public GameAudioCue LastCue { get; private set; }

        private void Awake()
        {
            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.8f;

            _muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            _source.mute = _muted;

            BuildClips();
        }

        /// <summary>
        /// Persists the mute preference and applies it immediately.
        /// </summary>
        public void SetMuted(bool muted)
        {
            _muted = muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();

            if (_source == null)
            {
                return;
            }

            _source.mute = muted;
            if (muted)
            {
                _source.Stop();
            }
        }

        /// <summary>
        /// Flips the mute preference.
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!_muted);
        }

        /// <summary>
        /// Plays a cue when audio is enabled. Muted calls do not count as
        /// played cues; a missing source or clip skips audio output only.
        /// </summary>
        public void Play(GameAudioCue cue)
        {
            if (_muted)
            {
                return;
            }

            LastCue = cue;
            PlayedCueCount++;

            var index = (int)cue;
            if (_source == null || index < 0 || index >= _clips.Length)
            {
                return;
            }

            var clip = _clips[index];
            if (clip != null)
            {
                _source.PlayOneShot(clip);
            }
        }

        private void BuildClips()
        {
            _clips[(int)GameAudioCue.Button] = CreateTone(
                "GameAudio.Button",
                880f,
                880f,
                0.05f,
                Amplitude * 0.8f);
            _clips[(int)GameAudioCue.Kill] = CreateTone(
                "GameAudio.Kill",
                520f,
                180f,
                0.10f,
                Amplitude);
            _clips[(int)GameAudioCue.Hit] = CreateTone(
                "GameAudio.Hit",
                190f,
                90f,
                0.09f,
                Amplitude * 1.2f);
            _clips[(int)GameAudioCue.LevelUp] = CreateSequence(
                "GameAudio.LevelUp",
                new[] { 523.25f, 659.25f, 783.99f },
                0.06f);
            _clips[(int)GameAudioCue.Reward] = CreateSequence(
                "GameAudio.Reward",
                new[] { 587.33f, 880f },
                0.07f);
            _clips[(int)GameAudioCue.Victory] = CreateSequence(
                "GameAudio.Victory",
                new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
                0.08f);
            _clips[(int)GameAudioCue.Defeat] = CreateSequence(
                "GameAudio.Defeat",
                new[] { 392f, 329.63f, 261.63f, 196f },
                0.09f);
        }

        private static AudioClip CreateTone(
            string name,
            float startFrequency,
            float endFrequency,
            float durationSeconds,
            float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[sampleCount];
            var phase = 0f;
            var attackSamples = Mathf.Max(1, sampleCount / 10);
            var releaseSamples = Mathf.Max(1, sampleCount / 4);

            for (var i = 0; i < sampleCount; i++)
            {
                var progress = (float)i / sampleCount;
                var frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += (2f * Mathf.PI * frequency) / SampleRate;
                var envelope = Mathf.Min(1f, i / (float)attackSamples) *
                               Mathf.Min(1f, (sampleCount - i) / (float)releaseSamples);
                data[i] = Mathf.Sin(phase) * amplitude * envelope;
            }

            return CreateClip(name, data);
        }

        private static AudioClip CreateSequence(
            string name,
            float[] frequencies,
            float noteSeconds)
        {
            var noteSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * noteSeconds));
            var sampleCount = noteSamples * frequencies.Length;
            var data = new float[sampleCount];
            var phase = 0f;
            var attackSamples = Mathf.Max(1, noteSamples / 10);
            var releaseSamples = Mathf.Max(1, noteSamples / 4);

            for (var note = 0; note < frequencies.Length; note++)
            {
                for (var i = 0; i < noteSamples; i++)
                {
                    phase += (2f * Mathf.PI * frequencies[note]) / SampleRate;
                    var envelope = Mathf.Min(1f, i / (float)attackSamples) *
                                   Mathf.Min(1f, (noteSamples - i) / (float)releaseSamples);
                    data[(note * noteSamples) + i] = Mathf.Sin(phase) * Amplitude * envelope;
                }
            }

            return CreateClip(name, data);
        }

        private static AudioClip CreateClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
