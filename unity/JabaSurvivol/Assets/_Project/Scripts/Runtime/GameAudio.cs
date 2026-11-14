using System;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Audio cue identifiers. Every cue maps to a procedural clip generated at
    /// runtime so the project does not depend on imported audio assets.
    /// </summary>
    public enum GameAudioCue
    {
        Hit,
        Kill,
        LevelUp,
        Reward,
        Victory,
        Defeat,
        Impact,
        BossHit,
        PistolFire,
        SmgFire,
        ShotgunFire,
        RifleFire,
        LmgFire,
        Empty,
        ReloadStart,
        ReloadComplete,
        WeaponSwitch,
        TurretDeploy,
        TurretFire,
        Dash,
        Shockwave,
        BossSpawn,
        BossTelegraph,
        BossSlam,
        BossEnrage,
        BossDeath,
        RoundStart,
        RoundClear,
        UiClick,
        UiOpen,
        UiClose,
        Purchase,
        Error
    }

    /// <summary>
    /// Lightweight procedural audio feedback. Clips are generated once and
    /// cached. Every cue shares a fixed source pool, a master mute, a master
    /// volume, and an effects volume. The preference values are persisted in
    /// <see cref="PlayerPrefs"/>.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public const string MutedKey = "audio.muted";
        public const string MasterVolumeKey = "audio.masterVolume";
        public const string EffectsVolumeKey = "audio.effectsVolume";

        public const float DefaultMasterVolume = 1f;
        public const float DefaultEffectsVolume = 0.8f;
        public const int VoiceCount = 12;

        private const int SampleRate = 22050;
        private const float Amplitude = 0.12f;
        private const float CueCooldownSeconds = 0.03f;
        private static readonly int CueCount = Enum.GetValues(typeof(GameAudioCue)).Length;

        private readonly AudioClip[] _clips = new AudioClip[CueCount];
        private readonly AudioSource[] _sources = new AudioSource[VoiceCount];
        private readonly float[] _lastPlayTimes = new float[CueCount];
        private readonly GameAudioCue[] _recentCues = new GameAudioCue[8];
        private int _recentCueCount;
        private bool _initialized;
        private bool _muted;
        private bool _suspended;

        /// <summary>
        /// True when cue output is muted. The preference is persisted.
        /// </summary>
        public bool IsMuted => _muted;

        /// <summary>
        /// True while the game or application has suspended audio.
        /// </summary>
        public bool IsSuspended => _suspended;

        /// <summary>
        /// Master volume in the range 0 to 1. The preference is persisted.
        /// </summary>
        public float MasterVolume { get; private set; } = DefaultMasterVolume;

        /// <summary>
        /// Effects volume in the range 0 to 1. The preference is persisted.
        /// </summary>
        public float EffectsVolume { get; private set; } = DefaultEffectsVolume;

        /// <summary>
        /// Effective output volume after mute and suspend state are applied.
        /// </summary>
        public float EffectiveVolume => _muted || _suspended
            ? 0f
            : Mathf.Clamp01(MasterVolume) * Mathf.Clamp01(EffectsVolume);

        /// <summary>
        /// Number of audible cues requested since this instance started.
        /// </summary>
        public int PlayedCueCount { get; private set; }

        /// <summary>
        /// The most recent cue requested while audible.
        /// </summary>
        public GameAudioCue LastCue { get; private set; }

        public int CueCountForTests => CueCount;

        public int VoiceCountForTests => _sources.Length;

        public int ActiveVoiceCountForTests
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _sources.Length; i++)
                {
                    if (_sources[i] != null && _sources[i].isPlaying)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the service for EditMode tests that do not run Awake.
        /// </summary>
        public void InitializeForTests()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            for (var i = 0; i < CueCount; i++)
            {
                _lastPlayTimes[i] = -1000f;
            }

            _muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            MasterVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            EffectsVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultEffectsVolume));

            for (var i = 0; i < _sources.Length; i++)
            {
                var sourceObject = new GameObject($"Audio Source {i + 1}");
                sourceObject.transform.SetParent(transform, false);
                var source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.volume = EffectiveVolume;
                source.mute = _muted || _suspended;
                _sources[i] = source;
            }

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

            if (muted)
            {
                StopAll();
            }

            ApplySourceSettings();
        }

        /// <summary>
        /// Flips the mute preference.
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!_muted);
        }

        /// <summary>
        /// Sets and persists the master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.Save();
            ApplySourceSettings();
        }

        /// <summary>
        /// Sets and persists the effects volume.
        /// </summary>
        public void SetEffectsVolume(float volume)
        {
            EffectsVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(EffectsVolumeKey, EffectsVolume);
            PlayerPrefs.Save();
            ApplySourceSettings();
        }

        /// <summary>
        /// Suspends or resumes cue output. Suspending stops active one-shots.
        /// The persisted mute and volume preferences are not changed.
        /// </summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
            if (suspended)
            {
                StopAll();
            }

            ApplySourceSettings();
        }

        /// <summary>
        /// Stops every active one-shot.
        /// </summary>
        public void StopAll()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i]?.Stop();
            }
        }

        /// <summary>
        /// Plays a cue when audio is enabled. Muted, suspended, or zero-volume
        /// calls do not count as played cues. Missing sources or clips skip
        /// output only.
        /// </summary>
        public void Play(GameAudioCue cue)
        {
            if (_muted || _suspended || EffectiveVolume <= 0f)
            {
                return;
            }

            var index = (int)cue;
            if (index < 0 || index >= _clips.Length)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - _lastPlayTimes[index] < CueCooldownSeconds)
            {
                return;
            }

            var clip = _clips[index];
            if (clip == null)
            {
                return;
            }

            var source = GetFreeSource();
            if (source == null)
            {
                return;
            }

            source.volume = EffectiveVolume;
            source.PlayOneShot(clip);
            _lastPlayTimes[index] = now;
            LastCue = cue;
            PlayedCueCount++;
            _recentCues[_recentCueCount % _recentCues.Length] = cue;
            _recentCueCount++;
        }

        public int RecentCueCountForTests => _recentCueCount;

        public GameAudioCue RecentCueForTests(int offsetFromNewest)
        {
            if (offsetFromNewest < 0 || offsetFromNewest >= _recentCues.Length || offsetFromNewest >= _recentCueCount)
            {
                return default;
            }

            var index = (_recentCueCount - 1 - offsetFromNewest) % _recentCues.Length;
            if (index < 0)
            {
                index += _recentCues.Length;
            }

            return _recentCues[index];
        }

        public void ResetHistoryForTests()
        {
            _recentCueCount = 0;
            for (var i = 0; i < _lastPlayTimes.Length; i++)
            {
                _lastPlayTimes[i] = -1000f;
            }
        }

        public bool HasClipForTests(GameAudioCue cue)
        {
            var index = (int)cue;
            return index >= 0 && index < _clips.Length && _clips[index] != null;
        }

        public int ClipSampleCountForTests(GameAudioCue cue)
        {
            var index = (int)cue;
            return index >= 0 && index < _clips.Length && _clips[index] != null
                ? _clips[index].samples
                : 0;
        }

        public int ClipSampleRateForTests(GameAudioCue cue)
        {
            var index = (int)cue;
            return index >= 0 && index < _clips.Length && _clips[index] != null
                ? _clips[index].frequency
                : 0;
        }

        private AudioSource GetFreeSource()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] != null && !_sources[i].isPlaying)
                {
                    return _sources[i];
                }
            }

            return null;
        }

        private void ApplySourceSettings()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] == null)
                {
                    continue;
                }

                _sources[i].mute = _muted || _suspended;
                _sources[i].volume = EffectiveVolume;
            }
        }

        private void BuildClips()
        {
            _clips[(int)GameAudioCue.Hit] = CreateTone(
                "GameAudio.Hit",
                190f,
                90f,
                0.09f,
                Amplitude * 1.2f);
            _clips[(int)GameAudioCue.Kill] = CreateTone(
                "GameAudio.Kill",
                520f,
                180f,
                0.10f,
                Amplitude);
            _clips[(int)GameAudioCue.LevelUp] = CreateSequence(
                "GameAudio.LevelUp",
                new[] { 523.25f, 659.25f, 783.99f },
                0.06f,
                Amplitude);
            _clips[(int)GameAudioCue.Reward] = CreateSequence(
                "GameAudio.Reward",
                new[] { 587.33f, 880f },
                0.07f,
                Amplitude);
            _clips[(int)GameAudioCue.Victory] = CreateSequence(
                "GameAudio.Victory",
                new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
                0.08f,
                Amplitude);
            _clips[(int)GameAudioCue.Defeat] = CreateSequence(
                "GameAudio.Defeat",
                new[] { 392f, 329.63f, 261.63f, 196f },
                0.09f,
                Amplitude);
            _clips[(int)GameAudioCue.Impact] = CreateNoise(
                "GameAudio.Impact",
                0.05f,
                Amplitude * 0.9f);
            _clips[(int)GameAudioCue.BossHit] = CreateTone(
                "GameAudio.BossHit",
                120f,
                70f,
                0.08f,
                Amplitude * 1.3f);
            _clips[(int)GameAudioCue.PistolFire] = CreateNoise(
                "GameAudio.PistolFire",
                0.07f,
                Amplitude * 1.1f);
            _clips[(int)GameAudioCue.SmgFire] = CreateNoise(
                "GameAudio.SmgFire",
                0.045f,
                Amplitude * 0.8f);
            _clips[(int)GameAudioCue.ShotgunFire] = CreateNoise(
                "GameAudio.ShotgunFire",
                0.11f,
                Amplitude * 1.5f);
            _clips[(int)GameAudioCue.RifleFire] = CreateNoise(
                "GameAudio.RifleFire",
                0.085f,
                Amplitude * 1.25f);
            _clips[(int)GameAudioCue.LmgFire] = CreateNoise(
                "GameAudio.LmgFire",
                0.06f,
                Amplitude * 1.15f);
            _clips[(int)GameAudioCue.Empty] = CreateTone(
                "GameAudio.Empty",
                90f,
                70f,
                0.035f,
                Amplitude * 0.8f);
            _clips[(int)GameAudioCue.ReloadStart] = CreateTone(
                "GameAudio.ReloadStart",
                220f,
                160f,
                0.08f,
                Amplitude);
            _clips[(int)GameAudioCue.ReloadComplete] = CreateTone(
                "GameAudio.ReloadComplete",
                400f,
                620f,
                0.06f,
                Amplitude);
            _clips[(int)GameAudioCue.WeaponSwitch] = CreateTone(
                "GameAudio.WeaponSwitch",
                300f,
                520f,
                0.05f,
                Amplitude * 0.9f);
            _clips[(int)GameAudioCue.TurretDeploy] = CreateSequence(
                "GameAudio.TurretDeploy",
                new[] { 330f, 440f, 550f },
                0.045f,
                Amplitude);
            _clips[(int)GameAudioCue.TurretFire] = CreateTone(
                "GameAudio.TurretFire",
                260f,
                180f,
                0.035f,
                Amplitude * 0.7f);
            _clips[(int)GameAudioCue.Dash] = CreateTone(
                "GameAudio.Dash",
                180f,
                540f,
                0.10f,
                Amplitude);
            _clips[(int)GameAudioCue.Shockwave] = CreateTone(
                "GameAudio.Shockwave",
                95f,
                38f,
                0.14f,
                Amplitude * 1.4f);
            _clips[(int)GameAudioCue.BossSpawn] = CreateSequence(
                "GameAudio.BossSpawn",
                new[] { 110f, 146.83f, 98f },
                0.12f,
                Amplitude * 1.2f);
            _clips[(int)GameAudioCue.BossTelegraph] = CreateTone(
                "GameAudio.BossTelegraph",
                150f,
                120f,
                0.20f,
                Amplitude * 1.1f);
            _clips[(int)GameAudioCue.BossSlam] = CreateTone(
                "GameAudio.BossSlam",
                75f,
                30f,
                0.16f,
                Amplitude * 1.5f);
            _clips[(int)GameAudioCue.BossEnrage] = CreateSequence(
                "GameAudio.BossEnrage",
                new[] { 110f, 130.81f, 155.56f, 110f },
                0.08f,
                Amplitude * 1.25f);
            _clips[(int)GameAudioCue.BossDeath] = CreateSequence(
                "GameAudio.BossDeath",
                new[] { 196f, 164.81f, 130.81f, 98f },
                0.12f,
                Amplitude * 1.3f);
            _clips[(int)GameAudioCue.RoundStart] = CreateSequence(
                "GameAudio.RoundStart",
                new[] { 440f, 554.37f, 659.25f },
                0.05f,
                Amplitude);
            _clips[(int)GameAudioCue.RoundClear] = CreateSequence(
                "GameAudio.RoundClear",
                new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
                0.06f,
                Amplitude);
            _clips[(int)GameAudioCue.UiClick] = CreateTone(
                "GameAudio.UiClick",
                820f,
                620f,
                0.02f,
                Amplitude * 0.55f);
            _clips[(int)GameAudioCue.UiOpen] = CreateTone(
                "GameAudio.UiOpen",
                500f,
                700f,
                0.04f,
                Amplitude * 0.7f);
            _clips[(int)GameAudioCue.UiClose] = CreateTone(
                "GameAudio.UiClose",
                700f,
                500f,
                0.04f,
                Amplitude * 0.7f);
            _clips[(int)GameAudioCue.Purchase] = CreateSequence(
                "GameAudio.Purchase",
                new[] { 659.25f, 880f },
                0.05f,
                Amplitude);
            _clips[(int)GameAudioCue.Error] = CreateTone(
                "GameAudio.Error",
                220f,
                110f,
                0.12f,
                Amplitude * 0.9f);
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] == null)
                {
                    continue;
                }

                Destroy(_clips[i]);
                _clips[i] = null;
            }
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
            float noteSeconds,
            float amplitude)
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
                    data[(note * noteSamples) + i] = Mathf.Sin(phase) * amplitude * envelope;
                }
            }

            return CreateClip(name, data);
        }

        private static AudioClip CreateNoise(
            string name,
            float durationSeconds,
            float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[sampleCount];
            var state = 0x9E3779B9u;
            var attackSamples = Mathf.Max(1, sampleCount / 20);
            var releaseSamples = Mathf.Max(1, sampleCount / 2);

            for (var i = 0; i < sampleCount; i++)
            {
                state = unchecked((state * 1664525u) + 1013904223u);
                var noise = (((state >> 8) & 0xFFFFu) / 32768f) - 1f;
                var envelope = Mathf.Min(1f, i / (float)attackSamples) *
                               Mathf.Min(1f, (sampleCount - i) / (float)releaseSamples);
                data[i] = noise * amplitude * envelope;
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
