---
description: Verify acceptance criteria, tests, builds, and release readiness.
agent: verifier
---

Verify this work: $ARGUMENTS

Read `AGENTS.md`, the issue acceptance criteria, and the pull request.

Verification scope:

- Prefer the smallest evidence that can prove the change: focused EditMode or
  PlayMode results during iteration, full suites and a production ARM64 APK at
  the PR or release gate.
- Reuse the warm Unity workspace. Do not scan `Library`, `Temp`, `Logs`,
  `Builds`, `obj`, or `bin`.
- Read the result XML and the tail of the Unity log. Do not dump full logs.
- Record the test filter, duration, result file, and emulator or device label.

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
