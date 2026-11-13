# ADR-0010: Generated Art Pipeline

- Status: Accepted
- Date: 2026-11-13
- Related: v0.7.0 Art and Visual Polish, `docs/standards/art-direction.md`

## Context

The game uses runtime primitives for actors, weapons, projectiles, effects, and
environment. The visual set must become readable on phones and tablets without
adding a hand-authored asset dependency that is difficult to reproduce, review,
or regenerate.

The repository already generates procedural audio at runtime and keeps gameplay
rules in `Game.Core`. The art pipeline should follow the same principles:
deterministic, reviewable, and independent from gameplay code.

## Decision

- Use a deterministic in-repo generator at `tools/ArtGenerator`.
- Rasterize PNG sprites with SkiaSharp as a tool-only dependency.
- Commit the generated PNGs and the manifest; do not require the generator at
  runtime or during a player build.
- Import assets through an Editor postprocessor that reads the manifest for
  pixels-per-unit, pivots, filtering, and compression.
- Load sprites through a cached `ArtCatalog` from `Resources` with a safe
  primitive fallback.
- Keep gameplay, balance, hitboxes, and persistent progression unchanged.
- Do not hand-edit generated PNGs. Change the generator, regenerate, review the
  contact sheet, and commit the output and manifest together.

## Consequences

- The visual set is reproducible and can be reviewed as source.
- The generator and its tests add build-tooling surface, not runtime surface.
- SkiaSharp must be recorded as a tool-only dependency and must build on Windows
  and Linux CI.
- Artists can replace individual sprites later by updating the manifest and
  import settings without changing gameplay code.
- Binary assets remain under Git LFS and are excluded from gameplay assemblies.
