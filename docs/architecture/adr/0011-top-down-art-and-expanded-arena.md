# ADR-0011: Top-Down Art and Expanded Arena

- Status: Accepted
- Date: 2026-11-14
- Owners: Architecture and art direction
- Related issues: #191, #192, #193, #194, #195, #196, #197

## Context

The v0.7.0 art set gave every actor authored parts, but the actor rig is a
side-view humanoid rotated toward aim. Zombies do not read as top-down threats
on a phone screen. The arena is `8.8 x 4.7` world units while the 16:9 camera
view is about `19.9 x 11.2`, so the player reaches visible walls at the screen
edge. The background uses one tiled floor and six props.

The next visual step must improve readability, sell the neon urban setting, and
keep the game playable on Android phones and tablets. It must not change
persistent progression, save data, weapons, skills, enemies, or rounds.

## Decision

1. Actors are authored and rigged as top-down silhouettes.
2. The player, Walker, Runner, Brute, and Warden keep modular parts: shadow,
   legs, torso, head, arms, and an optional weapon or core.
3. Zombie arms reach forward. The player weapon extends forward of the torso.
4. The arena extends beyond the camera view. The initial target is
   `ArenaHalfWidth = 18` and `ArenaHalfHeight = 10` (`36 x 20` units).
5. The camera follows the player with `0.12 s` smoothing and clamps so the view
   never shows outside the arena. The visible height is 11.2 world units and
   the half width is 5.6 * aspect. The clamp uses the arena half extents minus
   the camera half extents on each axis.
6. Enemies spawn on a ring around the player. The ring is
   clamp(max(halfWidth, halfHeight) + 1.5, 11.5, 14.0) world units, clamped to
   the arena inset. A clamped point closer than 10.5 units retries another
   angle.
7. The arena is composed of layers, in numeric order: base floor, floor grid,
   floor decals, light pools, props behind actors, perimeter, telegraphs,
   actors, and props in front.
8. Bloom, vignette, and 2D lights are performance-gated. The fallback must keep
   stable frame pacing on low-end Android devices.
9. `tools/ArtGenerator` remains the source of truth for every first-party asset.
10. v0.8.0 ships the reduced lighting fallback: camera post-processing and the
    global light are disabled after the Android washout. Follow-up #203 tracks
    the corrected Bloom/Vignette implementation.

## Consequences

- The actor generator and `CharacterVisual` rig must change together.
- Arena and spawn constants affect travel time and kiting, so the balance
  simulation and guard tests must run before release.
- New environment art needs an `Environment` atlas and ASTC compression.
- Camera follow and clamp need phone and tablet aspect-ratio tests.
- Lighting is not allowed to break the Android frame, memory, draw-call, or APK
  budgets. A reduced fallback ships if the full setup fails.
- Core gameplay rules, hitboxes, and persistent progression remain unchanged.
  Arena bounds and the spawn ring are the only approved balance-affecting
  changes and must pass the balance simulation and guard tests.
- Budget targets: at most `40` combat draw calls, `0 B/frame` steady-state GC
  allocation, 16.6 ms target frame time and 33.3 ms floor, 24 MB raw
  texture memory, and 10 MB APK art growth.

## Alternatives considered

- Keep side-view actors: rejected because the zombies do not read from above.
- Change only the background and keep the arena: rejected because the visible
  screen edge remains the gameplay limit.
- Extend the arena without camera follow: rejected because the player can walk
  off-screen.
- Spawn at the arena walls: rejected because enemies would take too long to
  reach the player in a larger arena.
- Add a new render pipeline or 3D lighting: rejected as unnecessary and too
  expensive for the Android target.

## Reversibility

The arena constants, camera rig, actor parts, and environment layout are
isolated behind data, named sorting orders, and the generated-art manifest. The
change can be reverted by restoring the prior constants and manifest without a
save migration.
