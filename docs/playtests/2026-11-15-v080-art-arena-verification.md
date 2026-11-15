# v0.8.0 Art and Arena Verification

- Date: 2026-11-15
- Scope: v0.8.0 Top-Down Art and Arena Upgrade
- Verification type: automated Unity suites, balance simulation, Android emulator smoke, screenshots, badging, hashes, filtered logcat, and performance probe
- Human playtest: none claimed

## Build identity

| Artifact | Version | Version code | ABI | Min API | Target API | Size | SHA-256 |
|---|---|---:|---|---:|---:|---:|---|
| Production APK | 0.8.0 | 10 | arm64-v8a | 26 | 36 | 35,776,013 bytes | `2bbdaf4725c399e572a61576f99fb77f8fe41034eaa1179d0ee08ee6635f601a` |
| Emulator APK | 0.8.0 | 10 | arm64-v8a, x86_64 | 26 | 36 | 82,595,060 bytes | `5b1efb0acec99ed05e97a197f9ab696611cd2d22b8f6138bae29a9249c7c94e6` |

The emulator APK was installed and launched. The production APK was badged and
hashed; it was not installed on the x86_64 emulator because it is ARM64-only.

## Automated verification

| Gate | Result |
|---|---|
| `dotnet build JabaSurvivol.slnx -c Release` | 0 warnings, 0 errors |
| `dotnet test JabaSurvivol.slnx -c Release` | 267 passed: 173 core, 86 integration, 1 architecture, 1 smoke, 6 art generator |
| `dotnet format JabaSurvivol.slnx --verify-no-changes` | passed |
| ArtGenerator tests | 6 passed |
| Unity EditMode | 120 passed, 0 failed |
| Unity PlayMode | 144 passed, 0 failed |
| Balance simulation | no outliers |

Unity coverage includes the top-down rig layout, atlas membership, environment
category output, deterministic arena positions, documented layer orders,
non-colliding props, camera follow and clamp, corner-safe spawn placement,
spawn-ring distance, visual-polish fallback, and the existing gameplay suites.

## Android emulator smoke

Device: `jaba_survivol_api35`, Android API 35 emulator.

| Step | Result |
|---|---|
| Install emulator APK | `Success` |
| Launch `com.jabasurvivol.game` | Activity started |
| Main menu | Rendered in English and Spanish |
| Gameplay | Top-down player, Walker, extended arena, props, and HUD rendered |
| Pause | Pause menu rendered |
| Phone layout | 2340 x 1080 |
| Tablet layout | 2560 x 1600 |
| Filtered app logcat | No `FATAL EXCEPTION` or `AndroidRuntime` crash |

## Performance probe

| Metric | Main menu | Gameplay |
|---|---:|---:|
| Draw calls | 18 | 32 |
| Steady-state GC allocation | 0 B/frame | 0 B/frame |
| Mono memory | 12.86 MB | 13.10 MB |
| Emulator total PSS (observed) | about 292 MB | about 292 MB |

The draw-call target is 40 and the observed gameplay value is 32. The emulator
PSS includes Unity, Android, and graphics-driver overhead and is recorded as an
observation, not as a device budget claim.

## Texture and APK budget

- Raw RGBA across all 96 assets: 26.45 MiB.
- Raw RGBA without the ASTC-compressed environment set: 15.64 MiB.
- Environment raw RGBA: 10.81 MiB; shipped as ASTC 6x6 in the Environment atlas.
- Estimated shipped texture memory: about 16.3 MiB, below the 24 MB target.
- APK growth over v0.7.1: 591,482 bytes (about 0.56 MiB), below the 10 MB target.

## Screenshot matrix

### Phone

- `v080-phone-main-en.png`
- `v080-phone-main-es.png`
- `v080-phone-gameplay-en.png`
- `v080-phone-gameplay-es.png`
- `v080-phone-pause-en.png`

### Tablet

- `v080-tablet-main-en.png`
- `v080-tablet-main-es.png`
- `v080-tablet-gameplay-en.png`
- `v080-tablet-gameplay-es.png`

### Actor and boss coverage

- `v080-rig-showcase.png` captures the top-down player, Walker, Runner, Brute,
  Warden, and turret lineup.
- The Warden is covered by PlayMode tests and the rig showcase; an in-game boss
  screenshot was not captured because the round-10 debug scenario ended before
  a stable capture.

## Evidence files

- `v080-emulator-badging.txt`
- `v080-emulator-sha256.txt`
- `v080-release-apk-badging.txt`
- `v080-release-apk-sha256.txt`
- `v080-install.txt`
- `v080-installed-package.txt`
- `v080-logcat-filtered.txt`
- `v080-perf-logcat.txt`
- `v080-perf-gameplay-logcat.txt`
- `v080-rig-showcase.png`

## Known limitations

- All Android evidence is from the local emulator. No physical phone or tablet
  test is claimed.
- Bloom and vignette are disabled in v0.8.0 because URP post-processing washed
  out the 2D scene on the Android emulator. Follow-up #203 tracks the corrected
  implementation.
- The arena and spawn-ring constants are the only balance-affecting changes; the
  balance simulation reports no outliers.
- Procedural audio remains placeholder audio. Final authored audio remains in
  v1.0.0.
