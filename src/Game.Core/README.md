# JabaSurvivol.Core

Pure C# game rules shared by the Unity client and the .NET backend.

Rules:

- No `UnityEngine` references.
- Deterministic and unit-testable.
- Targets .NET Standard 2.1 compatibility and Unity's C# 9 compiler.
- Contains stats, progression, combat math, rounds, economy, and save models.
- The same source is consumed by Unity through the local UPM package
  `com.jabasurvivol.core`. The package root is this directory; Unity compiles
  `Runtime/*.cs` through `Runtime/JabaSurvivol.Core.asmdef`.
- `.NET` intermediate and output files are redirected to `artifacts/` so Unity
  does not import a compiled copy of the assembly as a plugin.
