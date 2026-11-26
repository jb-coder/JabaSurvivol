# v1.0.0 Per-App Language Verification

- Date: 2026-11-26
- Issue: #144
- Branch: `feature/144-per-app-language`
- Code commit under test: `9170c8265551cf0ccd76b5b320ebb901e1dbef44`
- Result: pass

## Scope

This record covers Android 13+ per-app language support, the runtime system
locale refresh, Spanish accented-glyph coverage, and the phone and tablet smoke
checks for issue #144.

## Environment

- Unity `6000.3.28f1`.
- Android emulator images at API 35.
- Phone AVD `jaba_survivol_api35` (Pixel 5 profile), 1080x2340, 440 dpi.
- Tablet AVD `jaba_survivol_tablet_api35` (Pixel Tablet profile), 2560x1600,
  320 dpi.
- Development emulator APK with `arm64-v8a` and `x86_64` native code.

## Automated verification

```text
dotnet build JabaSurvivol.slnx -c Release
Result: 0 warnings, 0 errors.

dotnet test JabaSurvivol.slnx -c Release --no-build
Result: 272 passed, 0 failed.

dotnet format JabaSurvivol.slnx --verify-no-changes --no-restore
Result: passed.

Unity EditMode
Result: 137 passed, 0 failed.

Unity PlayMode
Result: 176 passed, 0 failed.

Android emulator build
Result: Success.
```

The APK at `artifacts/android/JabaSurvivol-emulator.apk` has:

- Size: 104,727,082 bytes.
- SHA-256: `78675CFB00C8EF0B9C57D993A647DBCE921127E6E5D5DD1CDA95B6C39E7B8FDD`.
- Package: `com.jabasurvivol.game`, versionCode `12`, versionName `0.9.0`.
- `minSdk 26`, `targetSdk 36`, `compileSdk 36`.
- Merged manifest attribute: `android:localeConfig="@xml/locales_config"`.
- Locale resource: `res/xml/locales_config.xml` with `en` and `es`.

## Device smoke checks

Phone:

```text
adb -s emulator-5554 shell cmd locale get-app-locales com.jabasurvivol.game
[] -> [en] -> [es]

adb -s emulator-5554 logcat -d -v brief | Select-String "FATAL EXCEPTION|E/AndroidRuntime|E/Unity"
0 matching lines
```

Tablet:

```text
adb -s emulator-5556 shell cmd locale get-app-locales com.jabasurvivol.game
[] -> [es] -> [en]

adb -s emulator-5556 logcat -d -v brief | Select-String "FATAL EXCEPTION|E/AndroidRuntime|E/Unity"
0 matching lines
```

The screenshots show the Spanish labels `CÓMO JUGAR`, `ARMERÍA`,
`CLASIFICACIÓN`, and `SESIÓN`, which confirms accented glyph rendering. The
tablet screenshots cover both English and Spanish.

## Evidence

- `docs/playtests/evidence/v100-per-app-language-phone.txt`
- `docs/playtests/evidence/v100-per-app-language-phone-es-help.png`
- `docs/playtests/evidence/v100-per-app-language-phone-es.png`
- `docs/playtests/evidence/v100-per-app-language-tablet.txt`
- `docs/playtests/evidence/v100-per-app-language-tablet-es-help.png`
- `docs/playtests/evidence/v100-per-app-language-tablet-es.png`
- `docs/playtests/evidence/v100-per-app-language-tablet-en.png`

## Not verified

- Physical phone and tablet hardware. All device evidence is emulator-based.
- Visual confirmation after an OS language toggle while the process remains
  alive. The focus and resume refresh path is covered by EditMode and PlayMode
  tests, not by an automated OS toggle on a running process.

## Result

The per-app language configuration, runtime refresh, explicit override
precedence, Spanish glyph coverage, and phone and tablet smoke checks pass for
the code commit under test.
