# Android Profiling Procedure

- Issue: #99
- Updated: 2026-11-12
- Scope: local Android emulator baseline; physical phone and tablet measurements
  remain part of #101.

## Emulator baseline

- Device: `sdk_gphone64_x86_64`
- Android API: 35
- Display: 1080 x 2340, landscape 2340 x 1080
- Memory: 2 GB
- ABI: x86_64
- Renderer: `swiftshader_indirect`, headless emulator
- APK: `JabaSurvivol-0.5.1-emulator.apk`
- SHA-256: `7D8D51F2D281F1DCADAEFDA3AC7F1F316C6C41F7E2541BD8E0D40A3896A8FB3F`
- Version: `0.5.1`, version code `6`
- After APK: `JabaSurvivol-0.5.1-emulator-perf.apk`
- After SHA-256: `9C1C3D9E7E7EEF637D4F41C9CFB8D82AF209AD6533768221C54516BE50ED79A6`

The software renderer is intentionally labelled. Its frame pacing is not a
physical-device claim and is not a release gate.

## Commands

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

# Install and cold start; use the perf APK for the optimized build
& $adb install -r -t artifacts/android/JabaSurvivol-0.5.1-emulator-perf.apk
& $adb shell am force-stop com.jabasurvivol.game
& $adb shell am start -W -n com.jabasurvivol.game/com.unity3d.player.UnityPlayerActivity

# Memory
& $adb shell dumpsys meminfo com.jabasurvivol.game

# Frame timing; use the SurfaceView layer printed by SurfaceFlinger --list
& $adb shell "dumpsys SurfaceFlinger --latency 'SurfaceView[com.jabasurvivol.game/com.unity3d.player.UnityPlayerActivity](BLAST)#127'"

# Logs
& $adb shell logcat -d -v brief
```

## Baseline results

### Cold start

`am start -W` reported:

```text
Status: ok
LaunchState: COLD
TotalTime: 6349
WaitTime: 6456
```

The 6.35-second cold start is inside the 10-second target.

### Memory

| Sample | Total PSS | Java heap | Native heap |
|---|---:|---:|---:|
| Main menu | 272,822 KB | 6,384 KB | 36,684 KB |
| Gameplay | 286,787 KB | 7,572 KB | 36,876 KB |
| 10-minute soak, 1 min | 231,305 KB | 8,036 KB | 23,352 KB |
| 10-minute soak, 10 min | 231,331 KB | 6,744 KB | 23,368 KB |

The 10-minute observation window stayed within 30 KB of PSS and 100 KB of the
Java heap after the first minute. No unbounded growth was observed.

The automated kiting script kept the process in round 1 for several minutes
before contact. The app then remained on the death screen for the rest of the
observation window. This is a memory soak, not a 10-minute survival claim; a
continuous active run and physical-device evidence remain in #101.

### Frame pacing

A 127-frame SurfaceFlinger sample during the soak reported:

| Metric | Value |
|---|---:|
| Refresh period | 16.67 ms |
| Average frame interval | 79.90 ms |
| 50th percentile | 79.99 ms |
| 95th percentile | 106.93 ms |
| 99th percentile | 129.24 ms |

This is approximately 12.5 FPS on the software-rendered headless emulator. It
does not meet the 30 FPS beta floor and must not be used as a device result.
Physical-device profiling stays in #101.

### After build

The optimized emulator APK was installed after the code changes:

| Metric | Before | After |
|---|---:|---:|
| Cold start | 6,349 ms | 2,305 ms |
| Main-menu PSS | 272,822 KB | 228,559 KB |
| Gameplay PSS | 286,787 KB | 284,598 KB |
| Frame p50 | 79.99 ms | 82.24 ms |
| Frame p95 | 106.93 ms | 113.65 ms |

The cold-start and main-menu differences include warm caches and a different
process state, so they are not a controlled A/B result. The frame difference is
inside the noise of the software renderer. The allocation and save-throttle
changes are verified by the PlayMode tests, not by the emulator FPS number.

The final stick-initialization guard was added after the after APK was built.
It is covered by the PlayMode suite and does not change the measured allocation
paths.

## Optimizations applied

- Profile saves use compact JSON instead of pretty-printed JSON.
- Kill and level-up progress marks the profile dirty and flushes it at most once
  every 0.75 seconds. Death, victory, pause, reward selection, meta actions,
  app pause, app quit, and destroy flush immediately.
- Telemetry payload strings are built only when telemetry is enabled and opted
  in. Archetype names use cached constants instead of `ToString().ToLower()`.
- HUD formatting uses integer overloads instead of `params object[]` and
  `reserve.ToString()`.
- Touch tracking uses one pass over active touches instead of two.
- The stick UI skips transform and color writes when the stick state has not
  changed.

## Verification

- Unity EditMode: 79 passed, 0 failed.
- Unity PlayMode: 87 passed, 0 failed.
- `dotnet build`, `dotnet test`, and `dotnet format`: pass.
- New regression test: rapid kills are saved at most once per interval.
- New regression test: profile JSON remains readable after the compact-format
  change.

## Remaining work

- Physical phone and tablet measurements, including thermal behavior, during
  #101.
- Continuous 10-minute active gameplay on a physical device.
- A frame-pacing comparison with a hardware-rendered emulator or device.

Raw profiler traces and APKs are not committed. Summaries and redacted evidence
live under `artifacts/android/` locally and are referenced by the issue.
