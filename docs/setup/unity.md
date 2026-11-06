# Unity Setup

## Current state

Unity Hub and Unity `6000.3.28f1` are installed with Android, Windows IL2CPP,
and WebGL build support at:

```text
C:\dev\personal\Unity\Hub\Editor\6000.3.28f1
```

The Unity project exists at `unity/JabaSurvivol` and was created from the
Universal 2D (URP) template. The project contains the walking skeleton described
in issue #3.

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

## Project configuration

The project is configured by `JabaSurvivol.Editor.ProjectSetup.Configure`:

- Universal 2D URP pipeline and 2D Renderer from `Assets/Settings`.
- Landscape left and right orientation, portrait disabled.
- Android IL2CPP, ARM64, target API 36, minimum API 26.
- Windows IL2CPP.
- WebGL IL2CPP.
- Input System only.
- Main scene at `Assets/_Project/Scenes/Main.unity`.

Run the configuration from the Unity menu or in batch mode:

```powershell
$unity = "C:\dev\personal\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe"
& $unity -batchmode -quit -projectPath unity/JabaSurvivol `
  -executeMethod JabaSurvivol.Editor.ProjectSetup.Configure
```

Commit `Assets`, `Packages/manifest.json`, `Packages/packages-lock.json`, and
`ProjectSettings`. Do not commit `Library`, `Temp`, `Logs`, `UserSettings`,
`Builds`, generated project files, or build output.

## Shared core package

`src/Game.Core` is both the .NET Standard 2.1 project and the local UPM package
`com.jabasurvivol.core`. The Unity project references it from
`Packages/manifest.json`:

```text
file:../../../src/Game.Core
```

The package source is compiled by Unity through
`Runtime/JabaSurvivol.Core.asmdef`. The .NET build output is redirected to
`artifacts/` by `src/Game.Core/Directory.Build.props` so Unity does not import a
compiled copy of the same assembly as a plugin.

Rules:

- No `UnityEngine` references.
- No Unity-specific types.
- Target .NET Standard 2.1 compatibility.
- Keep C# 9 syntax in the shared source so Unity can compile it.
- Unit tests run outside Unity.

## Tests

```powershell
$repoRoot = (Resolve-Path ".").Path
$unity = "C:\dev\personal\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe"
New-Item -ItemType Directory -Force "$repoRoot/artifacts/unity" | Out-Null

& $unity -batchmode -projectPath unity/JabaSurvivol `
  -runTests -testPlatform editmode `
  -testResults "$repoRoot/artifacts/unity/editmode.xml"

& $unity -batchmode -projectPath unity/JabaSurvivol `
  -runTests -testPlatform playmode `
  -testResults "$repoRoot/artifacts/unity/playmode.xml"
```

Unity test runs exit with code 0 when every test passes.

## Android build

Build the production ARM64 APK:

```powershell
pwsh tools/build/Build-Android.ps1 -OutputPath artifacts/android/JabaSurvivol.apk
```

Build an additional x86_64 + ARM64 APK for emulator smoke tests:

```powershell
$repoRoot = (Resolve-Path ".").Path
$unity = "C:\dev\personal\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe"
& $unity -batchmode -quit -projectPath unity/JabaSurvivol `
  -buildTarget Android `
  -executeMethod JabaSurvivol.Editor.BuildScript.BuildAndroidEmulator `
  -buildOutput "$repoRoot/artifacts/android/JabaSurvivol-emulator.apk"
```

Manual build settings:

- IL2CPP.
- ARM64 for production; ARM64 + x86_64 only for emulator builds.
- Target API 36.
- Minimum API 26.
- Landscape left and right.
- Debug keystore for internal builds.

Install and smoke test:

```powershell
$repoRoot = (Resolve-Path ".").Path
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb install -r "$repoRoot/artifacts/android/JabaSurvivol-emulator.apk"
& $adb logcat -c
& $adb shell am start -n com.jabasurvivol.game/com.unity3d.player.UnityPlayerActivity
& $adb logcat -d -v threadtime | Select-String "FATAL EXCEPTION|AndroidRuntime"
```

## CI

Set the repository variable `UNITY_CI_ENABLED=true` after the Unity project
exists and the following secrets are configured:

- `UNITY_LICENSE` or `UNITY_SERIAL`.
- `UNITY_EMAIL`.
- `UNITY_PASSWORD`.

The workflows use GameCI v6. If license activation fails, use local builds and
keep CI for backend tests until the license is fixed.

## Performance

- Profile on a physical Android device.
- Target 60 FPS on mid-range devices and 30 FPS minimum on low-end devices.
- Use object pooling.
- Avoid allocations in hot paths.
- Test phone and tablet aspect ratios and safe areas.
