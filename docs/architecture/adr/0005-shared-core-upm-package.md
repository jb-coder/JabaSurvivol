# ADR-0005: Shared Core as a Local UPM Package

## Status

Accepted.

## Context

ADR-0003 requires deterministic game rules in `src/Game.Core` to be shared by
the .NET backend and the Unity client. Duplicating the source or committing a
compiled assembly would create drift, and checking a compiled DLL into a UPM
package would hide source changes from review.

Unity can reference a local package whose root is outside the `Assets` folder.
The same directory can also be a normal .NET project when non-code files are
ignored by the compiler.

## Decision

- `src/Game.Core` is both the .NET Standard 2.1 project root and the local UPM
  package `com.jabasurvivol.core`.
- Unity consumes it through a `file:` dependency in
  `unity/JabaSurvivol/Packages/manifest.json`.
- Runtime code lives in `src/Game.Core/Runtime` and is compiled by
  `JabaSurvivol.Core.asmdef`.
- The `.NET` project redirects intermediate and output files to `artifacts/`
  through `src/Game.Core/Directory.Build.props` so Unity never imports a
  compiled copy of the same assembly as a plugin.
- Shared code stays within Unity's C# 9 compiler capabilities and has no
  `UnityEngine` references.

## Alternatives considered

- Duplicate the sources between `src/Game.Core` and a package folder. Rejected
  because the copies can drift and require a sync tool.
- Commit a compiled DLL into a UPM package. Rejected because generated output
  does not belong in source control and hides API changes from review.
- Keep the package under `unity/JabaSurvivol/Packages` and link sources with
  file-system links. Rejected because Windows checkouts do not reliably preserve
  symlinks.

## Consequences

- Unity and .NET compile the same files with one source of truth.
- Package changes are reviewed as normal source changes.
- The package must remain compatible with Unity's compiler and .NET Standard 2.1.
- Build output must stay outside the package directory.
