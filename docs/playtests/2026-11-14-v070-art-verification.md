# v0.7.0 Art Verification

- Date: 2026-11-14
- Scope: issues #160, #161, #162
- Build: `v0.7.0` working tree before release version bump
- Production APK: `artifacts/android/JabaSurvivol-v070-final.apk`
- Production SHA-256: `C1020582CEB1802560E37DE7CE8E0799E43F5CB654F7D2F4BBD11370D49623CE`
- Emulator APK: `artifacts/android/JabaSurvivol-v070-final-emulator.apk`
- Emulator SHA-256: `8080711F18809908071C29943B88240AD05E2B2D8CC0FDACEB48900AD8609350`

## Device and emulator

- Emulator: Android 15 (API 35), x86_64, `sdk_gphone64_x86_64`.
- Tablet layout: 2560 x 1600, density 240.
- Phone layout: 2340 x 1080, density 440.
- No physical-device result is claimed. Physical evidence remains tracked by #99.

## Automated verification

| Suite | Result |
|---|---|
| `dotnet build JabaSurvivol.slnx -c Release` | 0 warnings, 0 errors |
| ArtGenerator tests | 6 passed, 0 failed |
| Unity EditMode | 117 passed, 0 failed |
| Unity PlayMode | 110 passed, 0 failed |
| `ArtCatalogTests` | Manifest assets, pivots, FullRect environment, app and adaptive icons |
| `ArtAssetPresencePlayModeTests` | 30 required runtime keys and 60+ Resources sprites |
| `ArtUiIconPlayModeTests` | HUD, armory, reward, and active-skill icons |
| `CharacterVisualPlayModeTests` | Player aim, zombie pooling, elite aura, boss enrage, turret |

## Manifest and asset integrity

- 66 manifest entries.
- Every entry passes `ArtGenerator --check` with SHA-256, dimensions, and pivot validation.
- Every entry loads through `ArtCatalog` in EditMode.
- Raw RGBA texture budget: `16.77 MiB`, below the documented `24 MB` target.
- Production APK growth over v0.6.0: `961,740` bytes (`0.92 MiB`), below the `10 MB` target.
- Production APK size: `35,167,927` bytes.
- Emulator APK size: `101,888,392` bytes.

## Profiling

Captured with the development-only `PerformanceProbe` from a 10-minute emulator session:

```text
PERF drawCalls=12-20 gcAllocated=0 monoUsed=12.6-13.3 MB totalAllocated=91.8-92.3 MB
```

`gcAllocated` is the per-frame managed allocation reported by Unity. It stayed at `0`
in steady state. Draw calls stayed below the documented `40` target.

## Screenshot matrix

All screenshots are emulator captures, not physical-device captures.

| Surface | English | Spanish |
|---|---|---|
| Main menu | Not separately captured; English labels are covered by localization tests | `v070-emulator-main-es.png` |
| Gameplay HUD and arena | `v070-emulator-gameplay-arena-en.png` | `v070-phone-gameplay-es.png` |
| Armory | `v070-emulator-armory-en.png` | `v070-emulator-armory-es.png` |
| Reward cards | `v070-emulator-reward-en.png` | `v070-emulator-reward-es.png` |
| Pause | `v070-emulator-pause-en.png` | Shared pause layout |
| Character sheet | `v070-emulator-character-en.png` | Shared icon layout |
| Boss | `v070-emulator-boss-en.png` | Shared boss layout |
| Results | `v070-emulator-results-en.png` | `v070-phone-results-es.png` |
| Phone layout | `v070-phone-gameplay-es.png` | `v070-phone-main-es.png` |

All images are under `docs/playtests/evidence/`.

## Logcat

`v070-android-perf-logcat.txt` contains the filtered development session. It has
`PERF` samples and no `FATAL`, `AndroidRuntime`, or `NullReferenceException` lines.
Two non-fatal Unity shader log lines were observed at startup; they did not block
rendering and are not counted as a clean shader audit.

## Limitations

- Draw calls and allocation values come from the development emulator build.
- Physical phone and tablet evidence remains tracked in #99.
- Reward, boss, and character-sheet captures use development scenario hooks, not
  a full manual playthrough.
- No human playtest was performed.
- The full Spanish phone matrix covers main and results; other Spanish screens
  were captured on the tablet layout.

## Related evidence

- `v070-rig-showcase.png`
- `v070-emulator-gameplay-rigs.png`
- `v070-android-perf-logcat.txt`
- All `v070-emulator-*` and `v070-phone-*` screenshots.
