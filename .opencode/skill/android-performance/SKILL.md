---
name: android-performance
description: Use when building, profiling, or optimizing JabaSurvivol for Android phones and tablets, including IL2CPP, ARM64, touch UI, memory, and frame pacing.
---

# Android Performance

## Build settings

- IL2CPP, ARM64.
- Target API 36, minimum API 26.
- Landscape orientation.
- Universal 2D with URP 2D Renderer.
- Compressed textures and sprite atlases.
- Development builds only for profiling and cheats.

## Targets

- Mid-range Android: 60 FPS target.
- Low-end Android: 30 FPS minimum.
- No sustained thermal throttling during a 10-minute run.
- Stable memory with no growth across rounds.

## Hot paths

- Pool zombies, projectiles, VFX, and floating text.
- Avoid allocations in `Update`, `FixedUpdate`, physics callbacks, and UI refresh.
- Avoid `FindObjectOfType`, LINQ, string concatenation, and closures in hot paths.
- Cache component references.
- Limit concurrent zombies and projectiles.
- Use spatial partitioning or simple distance checks for large counts.

## UI and touch

- Safe Area on all screens.
- Large touch targets.
- Support phone and tablet aspect ratios.
- Test left-handed and right-handed layouts.
- Avoid overlapping controls and OS gesture areas.

## Profiling

- Profile on a physical device, not only the editor.
- Capture CPU, GPU, memory, draw calls, batches, and GC allocations.
- Compare against a baseline before and after changes.
- Record evidence in the PR.

## Verification

- Android APK builds.
- APK launches on emulator or device.
- `logcat` has no fatal errors.
- Screenshots for phone and tablet layouts.
- Performance evidence for changes that affect frame time or memory.
