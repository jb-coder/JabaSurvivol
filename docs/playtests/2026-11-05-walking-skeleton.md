# Walking Skeleton Verification

- Date: 2026-11-05
- Branch: `feature/unity-walking-skeleton`
- Build: `0.1.0` walking skeleton
- Unity: `6000.3.28f1`
- Backend: .NET SDK `10.0.401`

## Artifacts

| Artifact | Size | SHA-256 |
|---|---:|---|
| `artifacts/android/JabaSurvivol.apk` | 32,249,301 bytes | `4EE3340E257F07B89FFCA78ACFDB2033112701AA582CA0AEEF116C8853051BCB` |
| `artifacts/android/JabaSurvivol-emulator.apk` | 58,881,637 bytes | `E5A0CA189C68109D1E1B3E0E763E169B32FB17998110792AC5F6C0B5C0A39AE6` |

The production APK is ARM64 and IL2CPP. The emulator APK adds x86_64 for the
local Android emulator.

## Automated results

| Suite | Passed | Failed |
|---|---:|---:|
| .NET tests (`dotnet test JabaSurvivol.slnx -c Release`) | 20 | 0 |
| Unity EditMode | 5 | 0 |
| Unity PlayMode | 6 | 0 |

`dotnet build JabaSurvivol.slnx -c Release` completed with 0 warnings and
0 errors. `dotnet format JabaSurvivol.slnx --verify-no-changes` passed.

## Emulator smoke test

- Emulator: Android 15 (API 35), x86_64, Google Play system image.
- Phone profile: 1080 x 2400 at 420 dpi.
- Tablet profile: 2560 x 1600 at 320 dpi (display override).
- Install: `adb install -r artifacts/android/JabaSurvivol-emulator.apk` succeeded.
- Cold start: `com.jabasurvivol.game/com.unity3d.player.UnityPlayerActivity`
  became `topResumedActivity`.
- Logcat: no `FATAL EXCEPTION`, `AndroidRuntime` crash, or Unity exception
  from the application process.
- Checks performed: main menu, start run, HUD, touch sticks, firing, pause,
  resume, game over, restart, safe-area layout on phone and tablet profiles.
- Persistence across app restart is covered by the PlayMode test
  `Profile_survives_bootstrap_recreation`, which writes a save, destroys the
  bootstrap, and reloads supplies from the same file.

## Screenshots

- `evidence/phone-main-menu.png`
- `evidence/phone-playing.png`
- `evidence/tablet-main-menu.png`
- `evidence/tablet-playing.png`
- `evidence/tablet-pause.png`
- `evidence/tablet-game-over.png`

The screenshots are generated from the project build for verification and are
not third-party assets.

## Defects

No P0 or P1 defects found. The touch automation did not earn a kill in the
emulator session; kill rewards, death persistence, and restart behavior are
covered by the PlayMode suite.

## Verdict

Pass for the walking skeleton scope.
