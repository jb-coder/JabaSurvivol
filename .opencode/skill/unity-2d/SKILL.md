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

## Definition of done

- Touch works on phone and tablet.
- Tests pass.
- Android APK builds.
- No console or `logcat` errors.
- English-only repository content (approved locale tables excepted) and no-attribution policy respected.
