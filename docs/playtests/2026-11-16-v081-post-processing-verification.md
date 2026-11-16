# v0.8.1 Color-Safe Post-Processing Verification

- Date: 2026-11-16
- Scope: v0.8.1 Color-Safe Post-Processing
- Verification type: automated Unity suites, .NET suites, Android emulator smoke, screenshots, badging, hashes, filtered logcat, and performance probe
- Human playtest: none claimed

## Build identity

| Artifact | Version | Version code | ABI | Min API | Target API | Size | SHA-256 |
|---|---|---:|---|---:|---:|---:|---|
| Production APK | 0.8.1 | 11 | arm64-v8a | 26 | 36 | 35,959,243 bytes | `5C849DBCD36A616738AB3A96F38E5BD851BDACCB38C604A5EDA0ACA8EC2009AE` |
| Emulator APK | 0.8.1 | 11 | arm64-v8a, x86_64 | 26 | 36 | 104,616,007 bytes | `238E428A18D8BEFABA5999D0BCB32211901D3AEDBFB8DD8854F31DAF9DCA9D4F` |

The production APK is ARM64-only and was badged and hashed. It was not
installed on the x86_64 emulator. The emulator APK was installed and launched.

## Automated verification

| Gate | Result |
|---|---|
| `dotnet build JabaSurvivol.slnx -c Release` | 0 warnings, 0 errors |
| `dotnet test JabaSurvivol.slnx -c Release` | 267 passed: 173 core, 86 integration, 1 architecture, 1 smoke, 6 art generator |
| `dotnet format JabaSurvivol.slnx --verify-no-changes` | passed |
| Unity EditMode | 121 passed, 0 failed |
| Unity PlayMode | 146 passed, 0 failed |

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
| Draw calls | 20 to 34 |
| Steady-state GC allocation | 0 B/frame |
| Mono memory | about 13.1 MB |
| Emulator average frame time | 160 to 174 ms (SwiftShader) |

The draw-call target is 40 and the observed gameplay value is 34. The added
full-screen pass stays inside the draw-call and GC budgets.

The emulator is software-rendered and is not a device frame-time budget claim.
The v0.8.1 steady-state emulator sample is 160 to 174 ms/frame (about 6 FPS)
over the five-second probe windows. The startup sample (`4348.80` ms) is
excluded from the steady-state range. It does not satisfy the 16.6 ms device target and is
recorded as an observation only. Earlier emulator profiling in
`docs/setup/android-profiling.md` also recorded frame pacing outside the device
floor and must not be used as a device result. Physical phone and tablet
profiling remains in #99.

## APK budget

- v0.8.0 production APK: `35,776,001` bytes.
- v0.8.1 production APK: `35,959,243` bytes.
- Growth: `183,242` bytes (about `0.17 MiB`), below the `10 MB` target.

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

Phone evidence covers English and Spanish. Tablet evidence covers the Spanish
locale; tablet English screenshots were not captured for this patch release.

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
