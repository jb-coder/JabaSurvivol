---
description: Build a signed or debug Android APK and upload it as an artifact.
agent: devops
---

Build the Android APK for JabaSurvivol.

Read `AGENTS.md` and `docs/setup/unity.md`.

Workflow:

1. Confirm Unity 6.3 LTS and the Android module are available.
2. Confirm the Unity project exists at `unity/JabaSurvivol`.
3. Build with IL2CPP, ARM64, target API 36, minimum API 26, landscape orientation.
4. Use a debug keystore for internal builds. Never commit the keystore.
5. Upload the APK as a workflow artifact and, when releasing, attach it to the GitHub Release.
6. Report the build version, commit, artifact location, and any warnings.

Do not build from a dirty workspace without documenting the dirty state. Do not commit build output.
