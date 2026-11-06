# Unity Project

The Unity project lives at `unity/JabaSurvivol` and uses Unity `6000.3.28f1`
with the Universal 2D (URP) template. The first playable walking skeleton is
implemented in `Assets/_Project`.

## Project structure

- `Assets/_Project/Scenes/Main.unity`: single entry scene with `GameBootstrap`.
- `Assets/_Project/Scripts/Runtime`: gameplay, input, UI, save, and pooling.
- `Assets/_Project/Scripts/Editor`: project configuration and build entry points.
- `Assets/_Project/Tests/EditMode`: save and progression tests.
- `Assets/_Project/Tests/PlayMode`: bootstrap, combat, restart, and persistence tests.
- `Assets/Settings`: URP 2D renderer and pipeline assets.
- `Packages/manifest.json`: package dependencies and the local core package.

## Shared core package

`src/Game.Core` is both the .NET Standard 2.1 project and the local UPM package
`com.jabasurvivol.core`. `Packages/manifest.json` references it with:

```text
file:../../../src/Game.Core
```

Unity compiles `src/Game.Core/Runtime/*.cs` directly. The `.NET` build output is
redirected to `artifacts/` so Unity never imports a compiled copy of the same
assembly as a plugin.

## Verification

```powershell
dotnet build JabaSurvivol.slnx -c Release
dotnet test JabaSurvivol.slnx -c Release

$unity = "C:\dev\personal\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe"
& $unity -batchmode -projectPath unity/JabaSurvivol -runTests -testPlatform editmode -testResults artifacts/unity/editmode.xml
& $unity -batchmode -projectPath unity/JabaSurvivol -runTests -testPlatform playmode -testResults artifacts/unity/playmode.xml

pwsh tools/build/Build-Android.ps1
```

Use `JabaSurvivol.Editor.BuildScript.BuildAndroidEmulator` to produce an
additional x86_64 + ARM64 APK for local emulator smoke tests. Production builds
remain ARM64-only.

Do not commit `Library`, `Temp`, `Logs`, `UserSettings`, `Builds`, generated
project files, or build artifacts.
