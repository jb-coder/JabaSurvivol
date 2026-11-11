# Save and Cloud Hardening Verification

- Date: 2026-11-11
- Issue: #102
- Version under test: `0.5.0` (version code `5`)
- APK: `artifacts/android/JabaSurvivol-emulator-0.5.0-save-hardening.apk`
- APK SHA-256: `7FB1938DE22E00C03D1344FFCEABAFD6F20F68EF4254B7048F54F2F16E28CDE0`
- Android: API 35 x86_64 emulator, phone 1080x2340 at 440 dpi

## Changes verified

- The profile store recovers persistent progression from the backup file when
  the main save is empty or unreadable.
- A pending cloud-save conflict is cleared when the signed-in account changes.
- A local persistence failure during cloud backup or restore is reported as
  offline and never clears a pending conflict.

## Automated tests

| Suite | Result |
|---|---|
| .NET Release build | 0 warnings, 0 errors |
| .NET tests | 1 architecture, 167 core, 86 integration, 1 smoke passed; 0 failed |
| `dotnet format --verify-no-changes` | Clean |
| Unity EditMode | 77 passed, 0 failed |
| Unity PlayMode | 82 passed, 0 failed |

New coverage:

- `ProfileStoreTests`: recovery from an empty main save, a main save that
  deserializes to `null`, and a missing main save with an orphaned backup.
- `CloudSaveServiceTests`: `Reset` clears a pending conflict, an in-flight
  response is discarded after reset, and backup, restore, and keep-local
  persistence failures return offline without clearing the conflict or
  overwriting local progress.
- `CloudSavePlayModeTests`: signing out clears the conflict dialog.

## Android device verification

The emulator build was installed over the existing app data. The main save was
truncated to 0 bytes while `profile.json.bak` held the last valid save
(441 bytes, supplies 777, best round 3).

After launch:

- `profile.json` was restored to 441 bytes from the backup.
- The main menu showed `SUPPLIES 777` and `BEST ROUND 3`.

Evidence: `v051-save-recovery.txt`, `v051-0.5.0-phone-save-recovery.png`.

## Limitations

- The cloud conflict reset and restore persistence failure are covered by
  automated EditMode and PlayMode tests, not by a separate device run.
- No human playtest was performed.

## Conclusion

The #102 hardening changes are verified by automated tests and by an Android
emulator recovery run.
