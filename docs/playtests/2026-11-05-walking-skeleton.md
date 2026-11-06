# Walking Skeleton Verification

- Date: 2026-11-06
- Branch: `feature/unity-walking-skeleton`
- Build: `0.1.0` walking skeleton
- Unity: `6000.3.28f1`
- Backend: .NET SDK `10.0.401`

## Artifacts

| Artifact | Size | SHA-256 |
|---|---:|---|
| `artifacts/android/JabaSurvivol.apk` | 32,248,729 bytes | `8B1410DCEB8FC0C7628C586C77AC8DA9D2AADE75C10D1B070257FA3525777953` |
| `artifacts/android/JabaSurvivol-emulator.apk` | 58,886,625 bytes | `98D5EDED72DA65C4232CB38EB8812076997CD06603D3FC833ED080CDB72954AC` |

The production APK is ARM64 and IL2CPP. The emulator APK adds x86_64 for the
local Android emulator. Both are built at the repository root `artifacts/`
directory with an absolute output path.

## Automated results

| Suite | Passed | Failed |
|---|---:|---:|
| .NET tests (`dotnet test JabaSurvivol.slnx -c Release`) | 20 | 0 |
| Unity EditMode | 6 | 0 |
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
- Logcat: no `FATAL EXCEPTION`, `AndroidRuntime` crash, `NullReferenceException`,
  or `Exception` match in the app process log. Raw app log:
  `evidence/android-smoke-logcat.txt`.
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

The screenshots are captured from the project build for verification and are not
third-party assets.

## Artifact hosting note

The APK files are built locally and are not committed because build output must
not enter the repository. Unity CI is disabled; `UNITY_CI_ENABLED` is not set and
the Unity license CI secrets are not configured. The files can be reproduced
from source with the commands in `docs/setup/unity.md`, and the SHA-256 values
above identify the verified builds.

## Defects

No P0 or P1 defects found. The touch automation did not earn a kill in the
emulator session; kill rewards, death persistence, and restart behavior are
covered by the PlayMode suite.

## Verdict

Pass for the walking skeleton scope.
