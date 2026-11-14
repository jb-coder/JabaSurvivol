# v0.7.1 Audio Verification

- Date: 2026-11-14
- Scope: v0.7.1 Audio Review and Fixes
- Verification type: automated Unity suites, Android emulator smoke, screenshots, badging, hashes, and filtered logcat
- Human playtest: none claimed

## Build identity

| Artifact | Version | Version code | ABI | Min API | Target API | Size | SHA-256 |
|---|---|---:|---|---:|---:|---:|---|
| Production APK | 0.7.1 | 9 | arm64-v8a | 26 | 36 | 35,185,871 bytes | `6b6dc69efdc05f4cd225c418b0aadbf31dd48065e9098e000677d0252c3a0b7b` |
| Emulator APK | 0.7.1 | 9 | arm64-v8a, x86_64 | 26 | 36 | 118,001,563 bytes | `d24332b53be8f408e0409b73d040ad7d3ea936a41415a39a0303be1e45639ccd` |

The emulator APK was installed and launched. The production APK was badged
and hashed; it was not installed on the x86_64 emulator because it is
ARM64-only.

## Automated verification

| Gate | Result |
|---|---|
| `dotnet build JabaSurvivol.slnx -c Release` | 0 warnings, 0 errors |
| `dotnet test JabaSurvivol.slnx -c Release` | 267 passed: 173 core, 86 integration, 1 architecture, 1 smoke, 6 art generator |
| `dotnet format JabaSurvivol.slnx --verify-no-changes` | passed |
| Unity EditMode | 119 passed, 0 failed |
| Unity PlayMode | 133 passed, 0 failed |

New PlayMode coverage includes the complete 33-cue catalog, every weapon fire
cue, shotgun single-cue behavior, an empty magazine with zero reserve, reload
start and completion, weapon switch, player damage versus impact versus kill,
boss hit, turret deploy and fire, shockwave, defeat, reward, purchase, error,
UI click, open and close cues, volume clamping and persistence, zero-volume
silence, suspend and resume, voice limiting, source release, pause and
background lifecycle, the main-menu and pause-menu audio panels, and
pseudo-locale audio panel overflow.

## Android emulator smoke

Device: `jaba_survivol_api35`, Android API 35 emulator.

| Step | Result |
|---|---|
| Install emulator APK | `Success` |
| Launch `com.jabasurvivol.game` | Activity started |
| Main menu | Rendered in English and Spanish |
| Audio panel | Rendered on phone and tablet, English and Spanish |
| Mute toggle | Label changed from `SOUND: ON` to `SOUND: OFF` and back |
| Volume controls | `VOL 100%` to `VOL 90%`; `FX 80%` to `FX 70%` |
| Gameplay | Round 1 HUD, arena, player, zombie, and touch sticks rendered |
| Pause | Pause menu rendered and resumed |
| Background and resume | Home and relaunch returned to the same run |
| Filtered app logcat | No `FATAL EXCEPTION` or `AndroidRuntime` crash |
| AAudio lifecycle | Stream stopped on background and restarted on resume |

The app logcat shows `AAudioStream_requestStop` when the app goes to the
background and `AAudioStream_requestStart` after it returns. No audio error or
missing-clip line was observed.

## Screenshot matrix

### Phone

- `v071-phone-main-en.png`
- `v071-phone-main-es.png`
- `v071-phone-audio-panel-en.png`
- `v071-phone-audio-panel-es.png`
- `v071-phone-audio-muted-es.png`
- `v071-phone-gameplay-es.png`
- `v071-phone-pause-es.png`
- `v071-phone-pause-audio-es.png`
- `v071-phone-resume-es.png`

### Tablet

- `v071-tablet-main-en.png`
- `v071-tablet-main-es.png`
- `v071-tablet-audio-panel-en.png`
- `v071-tablet-audio-panel-es.png`

## Evidence files

- `v071-emulator-badging.txt`
- `v071-emulator-sha256.txt`
- `v071-release-apk-badging.txt`
- `v071-release-apk-sha256.txt`
- `v071-install.txt`
- `v071-installed-package.txt`
- `v071-logcat-filtered.txt`
- `v071-logcat-background-resume.txt`

## Known limitations

- All Android evidence is from the local emulator. No physical phone or tablet
  test is claimed.
- Procedural cues remain placeholder audio. Final authored music and production
  sound effects remain in v1.0.0.
- The production APK is ARM64-only and was not installed on the x86_64
  emulator.
- The audio panel uses language-neutral `VOL` and `FX` abbreviations to avoid
  adding unreviewed Spanish locale strings.
