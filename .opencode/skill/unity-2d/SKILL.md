---
name: unity-2d
description: Use when implementing or reviewing JabaSurvivol Unity 2D gameplay, touch controls, URP 2D rendering, assembly definitions, scenes, prefabs, and PlayMode tests.
---

# Unity 2D for JabaSurvivol

## Project baseline

- Unity 6.3 LTS (`6000.3.x`).
- Universal 2D template with URP 2D Renderer.
- Android-first, landscape, touch controls.
- Windows is a debugging and automated-test target.

## Fast loop

- Use the smallest Unity check that can fail: filtered EditMode for data and
  editor changes, filtered PlayMode for gameplay and UI, full suites before the
  PR.
- Reuse one warm workspace. Do not delete `Library`, `Library/Bee`, the IL2CPP
  cache, or the Gradle caches during normal iteration.
- Never recursively scan `Library`, `Temp`, `Logs`, `Builds`, `UserSettings`,
  `obj`, or `bin`. Use `git ls-files`, `glob`, or `grep` with explicit paths.
- Iterate in Editor PlayMode. Build a development emulator APK only for
  Android-visible changes. Build the production ARM64 APK at the PR or release
  gate.
- Run .NET tests and non-Unity work in parallel with Unity when they do not
  touch the same files.

## Architecture

- `Game.Core` is pure C# and must not reference `UnityEngine`.
- Keep assembly definitions per layer: Core, Runtime, UI, Network, Editor, Tests.
- Unity scripts should be thin adapters over core rules.
- Use ScriptableObjects for designer-tunable data and convert to core data at load time.
- Avoid singletons except for composition roots. Prefer injected services.

## Touch controls

- Left virtual stick: movement.
- Right virtual stick: aim and fire.
- Buttons: reload, weapon switch, active skill, pause.
- Support left-handed mode, joystick size/opacity, dead zone, auto-fire, and aim assist toggles.
- Respect Safe Area and support phone and tablet aspect ratios.
- Do not place interactive controls under notches or gesture areas.

## Rendering and performance

- URP 2D Renderer, sprite atlases, compressed textures, and limited overdraw.
- Pool zombies, projectiles, VFX, and floating text.
- Avoid allocations in `Update`, `FixedUpdate`, physics callbacks, and UI refresh paths.
- Limit concurrent enemies and projectiles.
- Profile on a real Android device before claiming performance work complete.

## Assets

- Commit every `.meta` file.
- Never commit `Library`, `Temp`, `Builds`, generated `.csproj`, or `.sln` files.
- Record every third-party asset in `THIRD_PARTY_NOTICES.md`.
- Use only assets with clear, compatible licenses.

## Tests

- EditMode: data conversion, save schema, configuration validation.
- PlayMode: movement, shooting, damage, rounds, shop, skills, persistence, and touch input simulation.
- Keep tests deterministic. Do not depend on frame timing when a deterministic clock can be used.
- Run the focused fixture during implementation and the full EditMode and
  PlayMode suites before the PR.
- Use `-testFilter` with the fixture or class name. Do not rerun everything
  after every edit.

## Definition of done

- Touch works on phone and tablet.
- Focused tests pass during implementation; full EditMode and PlayMode suites
  pass before the PR.
- Android APK builds for Android-visible changes; the production ARM64 APK
  passes at the PR or release gate.
- No console or `logcat` errors.
- Evidence records the test filter, result file, duration, and emulator or
  device label.
- English-only repository content (approved locale tables excepted) and no-attribution policy respected.
