---
description: Verify acceptance criteria, tests, builds, and release readiness.
agent: verifier
---

Verify this work: $ARGUMENTS

Read `AGENTS.md`, the issue acceptance criteria, and the pull request.

Check:

- Acceptance criteria with concrete evidence.
- Solution build and relevant tests.
- Android APK build for Android-visible changes.
- APK launch and `logcat` errors when an emulator is available.
- Touch controls, safe areas, phone/tablet layouts, and persistence.
- English-only repository content (approved locale tables excepted) and no-attribution policy.
- Required checks and reviewer approval.
- No P0 or P1 defects.

Report `PASS`, `FAIL`, or `BLOCKED` with evidence. Do not edit code. Do not claim manual human testing.
