---
description: Build and verify a playable candidate with automated smoke and gameplay evidence.
agent: verifier
---

Prepare a playable verification candidate: $ARGUMENTS

Read `AGENTS.md` and the target issue acceptance criteria.

Workflow:

1. Run the relevant unit, architecture, EditMode, and PlayMode tests.
2. Build the Android APK.
3. Install and launch the APK on an emulator when available; inspect `logcat` for fatal errors.
4. Capture screenshots for phone and tablet layouts when possible.
5. Verify touch controls, safe areas, landscape orientation, persistence, and the acceptance criteria.
6. Report `PASS`, `FAIL`, or `BLOCKED` with evidence.

Do not claim manual human testing. Do not edit code. Do not approve a build that fails to start or logs fatal errors.
