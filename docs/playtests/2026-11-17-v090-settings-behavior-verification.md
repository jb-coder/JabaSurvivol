# v0.9.0 Settings Behavior Verification

- Date: 2026-11-17
- Issues: #212, #213, #216
- Branch: `feature/212-216-settings-behavior`
- Base: `feature/211-unified-settings-panel`

## Scope

This slice applies the settings that #211 persisted and displayed:

- Left-handed touch layout.
- Joystick size and opacity.
- Optional aim assist.
- Auto-fire off with a manual fire control.
- Haptics on Android.
- Reduced motion.
- Colorblind-safe boss telegraph outline.

## Behavior verified

- Left-handed mode moves the movement stick to the right, the aim stick to the
  left, mirrors reload, switch, active skill, and pause, and updates the
  controls hint.
- Joystick size scales the base ring, knob, and touch radius together.
- Joystick opacity changes the visual alpha only.
- Aim assist uses the documented Low and High cones and ranges and falls back
  to raw aim outside them.
- Auto-fire off keeps the aim stick aiming only and shows a press-and-hold
  fire button.
- Haptics are off by default and only pulse for player damage, boss slam, and
  round clear when enabled.
- Reduced motion disables bloom and vignette immediately and at startup.
- Colorblind-safe telegraphs add a high-contrast outline without changing
  timing, radius, or damage.

## Verification

- `dotnet build JabaSurvivol.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test JabaSurvivol.slnx -c Release --no-build`: 267 passed, 0 failed.
- Unity EditMode: 129 passed, 0 failed.
- Unity PlayMode: 161 passed, 0 failed.
- One earlier PlayMode run had a single TearDown file-lock failure in
  `Round_reward_persists_across_bootstrap_recreation`; the isolated rerun and
  the next full run passed. No related code was changed.
- Android production APK: `JabaSurvivol-212-216.apk`, 35,984,251 bytes,
  SHA-256
  `5B72BBFA0930E310DFC745EDA7B70E8995F4BE95F0D3D7B57407D14E3528C842`.
- Android emulator APK: `JabaSurvivol-212-216-emulator.apk`, 83,057,954
  bytes, SHA-256
  `4204BFD26C66C75B57184953B724A45290125A7FA11B47A69E5CED1B5FACAEC1`.
- Both APKs declare `android.permission.VIBRATE` and
  `android.permission.INTERNET`.
- Both APKs expose
  `com.jabasurvivol.game/com.unity3d.player.UnityPlayerActivity` as the
  launchable activity.
- The emulator APK was installed and launched on `jaba_survivol_api35`.

## Screenshots

- `docs/playtests/evidence/v090-settings-phone-left-handed-autofire.png`
- `docs/playtests/evidence/v090-settings-phone-left-handed-hud-en.png`
- `docs/playtests/evidence/v090-settings-phone-left-handed-hud-es.png`

## Known limitations

- Android evidence is emulator-only. Physical phone and tablet validation
  remains tracked by #99.
- Haptic feedback requires a physical Android device; the emulator does not
  provide a perceptible vibration result.
- Aim assist is covered by automated tests and the settings UI, not by a
  visual capture.
- The Spanish settings and HUD strings require the pending human review in
  `docs/playtests/evidence/localization-review-es.txt`.
