# Unity Setup

## Current state

Unity Hub and Unity `6000.3.28f1` are installed with Android, Windows IL2CPP, and WebGL build support at:

```text
C:\dev\personal\Unity\Hub\Editor\6000.3.28f1
```

The Unity project itself still needs to be created with Unity Hub using the Universal 2D template. A Unity account and Personal license activation are required before opening or building projects.

Activate the license once:

1. Open Unity Hub.
2. Sign in with a Unity account.
3. Open Preferences > Licenses.
4. Add a free Personal license.
5. Retry project creation.

## Required versions

- Unity Hub.
- Unity 6.3 LTS (`6000.3.x`, current LTS supported through December 2027).
- Modules:
  - Android Build Support.
  - Android SDK and NDK Tools.
  - OpenJDK.
  - Windows Build Support (IL2CPP).
  - WebGL Build Support, optional.

Do not start the project on Unity 6.0 LTS; support ended in October 2026.

## Create the project

1. Open Unity Hub.
2. Create a new project with the **Universal 2D** template.
3. Use the project path `unity/JabaSurvivol`.
4. Set the company name and product name.
5. Open the project and enable the Input System when prompted.
6. Install the recommended packages:
   - Input System.
   - Cinemachine.
   - TextMeshPro.
   - 2D Tilemap.
   - 2D Animation.
   - Pixel Perfect.
   - Test Framework.
   - Code Coverage.
7. Set the project to landscape orientation.
8. Configure Android:
   - IL2CPP.
   - ARM64.
   - Target API 36.
   - Minimum API 26.
   - Development build for internal testing.
9. Commit `Assets`, `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings`.

## Shared core package

Create the local UPM package at:

```text
unity/JabaSurvivol/Packages/com.jabasurvivol.core
```

The package contains the pure C# game rules. The backend project `src/Game.Core` compiles the same source files to avoid duplication.

Rules:

- No `UnityEngine` references.
- No Unity-specific types.
- Target .NET Standard 2.1 compatibility.
- Unit tests run outside Unity.

## Android build

Use `tools/build/Build-Android.ps1` after Unity is installed and the project exists.

Manual build settings:

- IL2CPP.
- ARM64.
- Target API 36.
- Minimum API 26.
- Landscape left and right.
- Debug keystore for internal builds.

## CI

Set the repository variable `UNITY_CI_ENABLED=true` after the Unity project exists and the following secrets are configured:

- `UNITY_LICENSE` or `UNITY_SERIAL`.
- `UNITY_EMAIL`.
- `UNITY_PASSWORD`.

The workflows use GameCI v6. If license activation fails, use local builds and keep CI for backend tests until the license is fixed.

## Local checks

```powershell
pwsh tools/bootstrap/New-UnityProject.ps1 -CheckOnly
pwsh tools/build/Build-Android.ps1
```

## Performance

- Profile on a physical Android device.
- Target 60 FPS on mid-range devices and 30 FPS minimum on low-end devices.
- Use object pooling.
- Avoid allocations in hot paths.
- Test phone and tablet aspect ratios.
