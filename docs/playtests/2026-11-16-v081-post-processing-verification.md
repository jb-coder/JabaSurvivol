# v0.8.1 Color-Safe Post-Processing Verification

- Date: 2026-11-16
- Scope: v0.8.1 Color-Safe Post-Processing
- Verification type: automated Unity suites, .NET suites, Android emulator smoke, screenshots, badging, hashes, filtered logcat, and performance probe
- Human playtest: none claimed

## Build identity

| Artifact | Version | Version code | ABI | Min API | Target API | Size | SHA-256 |
|---|---|---:|---|---:|---:|---:|---|
| Production APK | 0.8.1 | 11 | arm64-v8a | 26 | 36 | 35,957,555 bytes | `8D124C84CEC5A8EFAB1D319D623973560AE6F2A2652F41097F793676E2E98993` |
| Emulator APK | 0.8.1 | 11 | arm64-v8a, x86_64 | 26 | 36 | 83,214,949 bytes | `7DF25B9EDF77E5D36038FED9D9294916E2D531608437EBE16E231C80194E07BA` |

The production APK is ARM64-only and was badged and hashed. It was not
installed on the x86_64 emulator. The emulator APK was installed and launched.

## Automated verification

| Gate | Result |
|---|---|
| `dotnet build JabaSurvivol.slnx -c Release` | 0 warnings, 0 errors |
| `dotnet test JabaSurvivol.slnx -c Release` | 267 passed: 173 core, 86 integration, 1 architecture, 1 smoke, 6 art generator |
| `dotnet format JabaSurvivol.slnx --verify-no-changes` | passed |
| Unity EditMode | 121 passed, 0 failed |
| Unity PlayMode | 145 passed, 0 failed |

Unity coverage includes the color-safe shader, material, and renderer feature
presence, the low-end fallback selection, the camera post-processing state, the
light budget, and the existing gameplay suites.

## Android emulator smoke

Device: `jaba_survivol_api35`, Android API 35 emulator, SwiftShader.

| Step | Result |
|---|---|
| Install emulator APK | Success |
| Launch `com.jabasurvivol.game` | Activity started |
| Main menu | Rendered in English and Spanish |
| Gameplay | Neon arena, top-down actors, bloom, and vignette rendered |
| Pause | Rendered in English and Spanish |
| Phone layout | 2340 x 1080 |
| Tablet layout | 2560 x 1600 |
| Filtered app logcat | No `FATAL EXCEPTION` or `AndroidRuntime` crash |
| Version | `0.8.1`, version code `11` |

The URP integrated camera post-processing path remains disabled. The
`JabaNeonPostFeature` full-screen pass runs before post-processing and replaces
the v0.8.0 beige washout with a readable dark neon scene.

## Performance probe

| Metric | Gameplay |
|---|---:|
| Draw calls | 15 to 30 |
| Steady-state GC allocation | 0 B/frame |
| Mono memory | about 13.3 MB |

The draw-call target is 40. The added full-screen pass stays inside the budget.

## Screenshot matrix

### Phone

- `v081-phone-menu-en.png`
- `v081-phone-menu-es.png`
- `v081-phone-gameplay-en.png`
- `v081-phone-gameplay-es.png`
- `v081-phone-pause-en.png`
- `v081-phone-pause-es.png`

### Tablet

- `v081-tablet-menu-es.png`
- `v081-tablet-gameplay-es.png`

## Evidence files

- `v081-emulator-badging.txt`
- `v081-emulator-sha256.txt`
- `v081-release-apk-badging.txt`
- `v081-release-apk-sha256.txt`
- `v081-logcat-filtered.txt`

## Known limitations

- All Android evidence is from the local emulator. No physical phone or tablet
  test is claimed.
- URP compatibility mode is a temporary workaround for the 2D color target.
  It must be removed when URP exposes a public Render Graph 2D color texture
  hook.
- The touch-stick rings are the existing v0.8.0 UI treatment and are not
  changed by this release.
- Procedural audio remains placeholder audio. Final authored audio remains in
  v1.0.0.
