# UI and Audio Feedback Verification

- Date: 2026-11-11
- Issue: #100
- Version under test: `0.5.0` (version code `5`)
- APK: `artifacts/android/JabaSurvivol-emulator-0.5.0-ui-polish.apk`
- APK SHA-256: `063A0369BAA16470C7A42F0AE6111282D0325D66F7B560C4D02AC62CF55481AC`
- Android: API 35 x86_64 emulator
  - Phone: 1080x2340 at 440 dpi
  - Tablet: 2560x1600 at 320 dpi

## Changes verified

- Procedural audio cues generated at runtime for hit, kill, level-up, reward,
  victory, and defeat. No imported audio assets were added.
- The mute preference is persisted in `PlayerPrefs` and gates every cue.
- The pause menu has a `SOUND: ON` / `SOUND: OFF` toggle at least 72 pixels
  tall.
- The pause menu was re-laid out so the sound toggle does not overlap the
  existing Resume, Armory, Skills, and Main Menu buttons.
- Every new user-facing string uses a localization key
  (`pause.sound.on`, `pause.sound.off`).

## Automated tests

| Suite | Result |
|---|---|
| .NET Release build | 0 warnings, 0 errors |
| .NET tests | 1 architecture, 171 core, 86 integration, 1 smoke passed; 0 failed |
| `dotnet format --verify-no-changes` | Clean |
| Unity EditMode | 77 passed, 0 failed |
| Unity PlayMode | 85 passed, 0 failed |

New PlayMode coverage:

- `Mute_preference_persists_across_instances`
- `Play_is_silent_and_uncounted_while_muted`
- `Pause_menu_sound_label_toggles_between_on_and_off`

## Android device verification

Phone and tablet screenshots show the pause menu and the sound toggle:

- `v051-ui-phone-pause-sound-on.png` and `v051-ui-phone-pause-sound-off.png`
- `v051-ui-tablet-pause-sound.png`
- `v051-ui-tablet-main.png` and `v051-ui-tablet-account.png`

The crash buffer is empty and the app log has no fatal exception, ANR,
AndroidRuntime crash, or NullReferenceException.

## Limitations

- No human playtest was performed.
- Audio cues are short procedural tones used as placeholders. Final audio
  production remains out of scope for this phase.
- Damage, kill, level-up, reward, victory, and defeat feedback is wired to the
  existing HUD and summary states; no new gameplay systems were added.

## Conclusion

The audio cues are consistent, the mute preference persists, and the pause
toggle is readable and reachable on phone and tablet layouts.
