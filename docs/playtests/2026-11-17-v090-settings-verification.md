# v0.9.0 Settings Panel Verification

- Date: 2026-11-17
- Issue: #211
- Scope: unified settings panel, audio and language entry-point consolidation, and device-local control and accessibility values
- Branch: `feature/211-unified-settings-panel`

## Verification

- `dotnet build JabaSurvivol.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test JabaSurvivol.slnx -c Release --no-build`: 267 passed, 0 failed.
- `dotnet format JabaSurvivol.slnx --verify-no-changes --no-restore`: passed.
- Unity EditMode: 129 passed, 0 failed.
- Unity PlayMode: 151 passed, 0 failed.
- Android production APK: `JabaSurvivol-211.apk`, 35,971,203 bytes, SHA-256 `F547A1B2C19295ABC92CA0632DA69D5C4D7F846BEA60EC707042F4F1706D579C`.
- Android emulator APK: `JabaSurvivol-211-emulator.apk`, 104,620,543 bytes, SHA-256 `86C011FB1F619A5C012FCBF945248CB8BD208E0763A3A5F4026E4D7AD0AEA8D5`.
- This slice was verified before the final version bump. The v0.9.0 release uses version `0.9.0` with version code `12`.

## Android evidence

- Emulator: Android 15 (API 35), `jaba_survivol_api35`.
- Phone display: 1080 x 2340 at 440 dpi, landscape.
- Tablet display override: 2560 x 1600 at 320 dpi.
- The settings panel was opened from the main menu on the emulator.
- PlayMode tests cover opening it from both the main menu and the pause menu.
- Audio mute and volume and language switching were exercised on the emulator.
- Control and accessibility persistence and reset are covered by the PlayMode
  tests.
- Filtered logcat contained no app crash or unhandled exception. See
  `docs/playtests/evidence/v090-settings-logcat.txt`.

## Screenshots

- `docs/playtests/evidence/v090-settings-phone-main.png`
- `docs/playtests/evidence/v090-settings-phone-en.png`
- `docs/playtests/evidence/v090-settings-phone-es.png`
- `docs/playtests/evidence/v090-settings-tablet-main-es.png`
- `docs/playtests/evidence/v090-settings-tablet-en.png`
- `docs/playtests/evidence/v090-settings-tablet-es.png`

## Known limitations

- Android evidence is emulator-only. Physical phone and tablet validation
  remains tracked by #99.
- The control and accessibility values are persisted and displayed in this
  slice. Left-handed layout, aim assist, reduced motion, haptics, and
  colorblind-safe telegraphs are wired in #212, #213, and #216.
- The Spanish v0.9.0 strings are approved in
  `docs/playtests/evidence/localization-review-es.txt`.
