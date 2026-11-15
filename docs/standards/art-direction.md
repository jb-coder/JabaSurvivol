# Art Direction

## Purpose

This document defines the visual rules for JabaSurvivol. Every generated,
imported, or commissioned asset must follow these rules before it can ship.

The target is a readable, neon-lit urban survival game for Android phones and
tablets in landscape orientation. Readability at phone size wins over detail.

## Setting

- Neon-lit urban survival in a sealed arena.
- Top-down view with strong silhouettes and a small palette.
- Actors and weapons are drawn facing right (`+X`). Aim rotation is
  `atan2(y, x)`; legs counter-rotate toward the movement direction.
- Friendly to low-end Android devices and to 1080p phone screens.
- No text baked into sprites. User-facing text uses localization keys.

## Palette

The palette has a dark base, one accent per combat role, and shared highlights.

| Token | Hex | Use |
|---|---|---|
| Background | `#04070C` | Camera clear color and negative space |
| Floor | `#0A0F18` | Arena floor base |
| Floor grid | `#141C2A` | Grid and floor seams |
| Outline | `#05080E` | Outer outline on every actor and prop |
| Shadow | `#000000` at 40% alpha | Ground contact shadow |
| Steel | `#6B7C93` | Weapon bodies and metal trim |
| White | `#F2FBFF` | Highlights and muzzle cores |
| Cyan | `#3DF2FF` | Player primary and selection |
| Ice | `#B8FBFF` | Player rim light |
| Amber | `#FFD24A` | Pistol, supplies, warnings |
| Orange | `#FF8A2A` | Runner and LMG accents |
| Red | `#FF3F4F` | Walker and danger telegraphs |
| Magenta | `#F25AE6` | Warden and rifle accents |
| Purple | `#A855F7` | Brute body and armor upgrades |
| Green | `#59FF9E` | Turret, healing, armor plates |
| Lime | `#C8FF66` | XP and progression highlights |
| Elite gold | `#FFE066` | Elite aura and ring |

Limit each sprite to one dominant accent plus neutral metal or cloth. Do not
rely on color alone to distinguish enemies. Silhouette and scale must carry the
archetype.

## Scale and resolution

- Reference pixels per unit: `256`.
- One world unit equals `256` reference pixels before scaling.
- Character parts: `256 x 256` for player, Walker, Runner, and Brute.
- Warden parts: `512 x 512`.
- Weapons: `256 x 128`, grip pivot near the left side.
- Projectiles: `128 x 64` or `64 x 64`, pivot center.
- Effects: `256 x 256`, pivot center.
- Icons: `128 x 128`, pivot center.
- Environment floor tiles: `512 x 512`. Decals and props: `256 x 256`. Walls:
  `256 x 64` or `64 x 256`. Light pools: `256 x 256`.
- PPU in Unity must match the manifest. Do not mix art authored at different
  PPUs without an explicit exception.

## Silhouette and lighting

- Light source: upper-left, cool white.
- Rim light: 2-4 reference pixels on the upper-left edge.
- Outer outline: 3-5 reference pixels of Outline color.
- Inner separation: 2 reference pixels of a darker local color.
- Ground shadow: soft ellipse below the actor, never used for collision.
- Visible height is more important than visible width. Keep heads, shoulders,
  and weapons from merging into one blob at 64 px.
- Test every combat sprite in grayscale at 64 px and at phone size.
- Avoid thin lines below 2 reference pixels; they vanish under ASTC.

## Actor silhouettes

| Actor | Required read |
|---|---|
| Player | Compact armored body, cyan visor, visible weapon arm |
| Walker | Rounded shoulders, uneven arms, red cloth |
| Runner | Lean torso, long arms, orange straps, forward lean |
| Brute | Wide shoulders, heavy arms, purple mass, small head |
| Warden | Massive shoulders, horned head, glowing magenta core |
| Turret | Low base, rotating head, green lens, short barrel |

The player must remain the brightest cyan silhouette on screen. Elites use the
gold aura and ring, not a body tint. The Warden uses a larger scale and a core
that shifts from magenta to red when enraged.

## Top-down actor rig

Actors are seen from above. The rig keeps the same part contract for the player,
Walker, Runner, Brute, and Warden so pooling and animation remain shared.

| Part | Top-down read |
|---|---|
| Shadow | Soft ellipse under the actor. |
| Leg | Long limb behind the torso, top pivot at the hip. |
| Torso | Shoulders, chest, and back armor seen from above. |
| Head | Top of the head, hood, helmet, or horns, slightly forward of center. |
| Arm | Forward-reaching limb with a hand, shoulder pivot near the torso. |
| Weapon | Top-down barrel and grip, forward of the torso. |
| Core | Warden only; glowing top-down core between the shoulders. |

- The player faces aim with the weapon forward.
- Zombie arms reach toward the target and sway during the walk.
- The head must not cover the weapon or the forward arm silhouette.
- The player remains the brightest cyan read on screen.
- Every actor must pass a 64 px grayscale silhouette test from above.

## Naming

Use lower snake case, ASCII only, with a category prefix:

- `char_player_torso`, `char_zombie_walker_head`
- `char_boss_warden_core`
- `char_turret_base`
- `weapon_pistol`, `weapon_smg`
- `projectile_pistol`, `projectile_pellet`
- `fx_muzzle_flash`, `fx_impact`, `fx_death_burst`, `fx_elite_aura`
- `reward_supplies`, `reward_xp`, `reward_upgrade`, `reward_ammo`, `reward_armor`
- `icon_skill_turret`, `icon_skill_dash`, `icon_skill_shockwave`
- `env_floor_tile`, `env_wall_horizontal`, `env_prop_barrel`
- `app_icon_foreground`, `app_icon_background`

Group by folder under `Assets/Resources/Art/`:

```text
Art/
  Characters/
  Weapons/
  Projectiles/
  Effects/
  Pickups/
  UI/
  Environment/
  App/
```

## Pivots

Manifest pivots are normalized `(0, 0)` bottom-left to `(1, 1)` top-right.

- Character parts and shadows: center `(0.5, 0.5)`.
- Legs: top-center `(0.5, 0.9)` so the foot swings from the hip.
- Weapons: grip `(0.18, 0.5)` so the barrel extends toward the aim direction.
- Projectiles and effects: center `(0.5, 0.5)`.
- Icons: center `(0.5, 0.5)`.
- Environment tiles: center `(0.5, 0.5)`.

Unity import must read the pivot from the manifest. Do not let Unity guess a
pivot from alpha bounds.

## Arena and camera

- Arena target: 36 x 20 world units (ArenaHalfWidth = 18,
  ArenaHalfHeight = 10).
- Camera height: orthographic size 5.6, so the visible height is 11.2
  world units. Visible half width is 5.6 * aspect.
- Camera target: SmoothDamp toward the player with `0.12 s` smoothing.
- Camera clamp: x = clamp(player.x, -18 + halfWidth, 18 - halfWidth) and
  y = clamp(player.y, -10 + halfHeight, 10 - halfHeight). If the view is
  wider or taller than the arena, center that axis.
- The camera never shows outside the perimeter walls on phone or tablet aspect
  ratios.
- Spawn ring: `ring = clamp(max(halfWidth, halfHeight) + 1.5, 11.5, 14.0)`.
  The 16:9 and 16:10 cases use 11.5. A 20:9 case uses about 13.9.
- Spawn point: pick a random angle, place it at the ring distance, clamp it to
  the arena inset, and retry if it is closer than 10.5 units to the player.
- Arena layers: base floor, floor grid, floor decals, light pools, props behind
  actors, actors, props in front, perimeter, and telegraphs.
- Prop categories: vehicles, street furniture, barricades, containers, debris,
  cables, and neon signs.
- Light pools: 256 x 256, alpha 30-60, placed under neon signs and above
  floor decals. No collider and no gameplay effect.
- Props and decals never create colliders.
- The lighting fallback must keep stable frame pacing on low-end Android.

## Sorting

Use numeric sorting orders from a single constants file.

| Layer | Order |
|---|---:|
| Floor | -40 |
| Floor grid | -35 |
| Floor decals | -32 |
| Light pools | -31 |
| Props behind actors | -20 |
| Perimeter walls and fences | -18 |
| Telegraphs | 10 |
| Death effects | 12 |
| Enemy shadows | 13 |
| Enemies | 15 |
| Elite aura | 14 |
| Elite ring | 16 |
| Boss shadow | 17 |
| Boss body | 18 |
| Boss core and ring | 19 |
| Player shadow | 19 |
| Player | 20 |
| Player weapon | 21 |
| Turret | 22 |
| Turret barrel | 23 |
| Props in front of actors | 24 |
| Projectiles | 30 |
| Muzzle and impact effects | 31 |

Character parts inside one rig may use the same order and rely on hierarchy.
Never change sorting to fix a one-off layering bug; add a named order instead.

## Lighting and post-processing

- Global ambient light stays dark enough for neon accents to read.
- Bloom is low-intensity and thresholded so only emissive highlights bloom.
- Vignette is subtle and never hides HUD or touch controls.
- 2D lights are limited to the player visor, muzzle flash, turret, Warden core,
  and selected neon props.
- At most eight lights may be active at once.
- Low-end devices use a reduced or disabled lighting profile.
- No custom shader graph is required for the first pass.

## Texture and compression

- Max texture size: `512` for characters and environment; `256` for icons.
- Filter mode: bilinear.
- Wrap mode: clamp.
- Mipmaps: off.
- Alpha is transparency: on.
- sRGB: on.
- Read/Write: off.
- Android character and weapon atlases: uncompressed or ASTC 6x6 if the result
  passes the neon-line test. Environment atlases may use ASTC 6x6.
- One atlas per category: `Characters`, `WeaponsFx`, `UI`, `Environment`.
- Keep total shipped art texture memory below `24 MB` on Android.
- Keep the shipped APK art increase below `10 MB`.

## Animation

Characters are modular sprite hierarchies animated with transforms, not
per-frame sprite sheets.

- Walk cycle: opposing leg swing, small torso bob, subtle arm counter-swing.
- Aim: body faces the aim direction; legs counter-rotate toward movement.
- Recoil: weapon mount and arms move back briefly on fire, then recover.
- Zombies: slower, uneven walk; runners use a faster cycle and forward lean.
- Warden: heavy bob and a core pulse that accelerates when enraged.
- Turret: idle base, head rotates toward target, barrel recoils on fire.
- Effects: pooled; no instantiate or destroy during combat.
- No per-frame allocations in `Update`, `FixedUpdate`, pooling, or UI.

## Quality gates

Every asset and every integration slice must pass:

- Silhouette test at 64 px, grayscale, on both phone and tablet crops.
- Palette check against this document; no untracked accent colors.
- Outline and rim-light consistency at 1x.
- No clipped glow, hard alpha halos, or sub-2-pixel neon lines.
- Contact sheet reviewed at 1x and phone size.
- English and Spanish screenshots when the asset appears in UI.
- Android build, install, launch, and filtered logcat.
- Camera follow, clamp, spawn-ring, and arena-layer tests.
- Combat draw calls at or below 40; steady-state GC allocation `0 B/frame`;
  frame time at or below 16.6 ms target and 33.3 ms floor; raw texture
  memory at or below 24 MB; APK art growth at or below 10 MB.
- Core gameplay rules, hitboxes, and persistent progression unchanged. Arena
  bounds and the spawn ring are the only approved balance-affecting changes and
  must pass the balance simulation and guard tests.

## Provenance

All assets are first-party and generated by `tools/ArtGenerator` unless a later
ADR approves a third-party source.

- The generator is the source of truth.
- Each committed PNG is listed in the manifest with its hash.
- The manifest records path, dimensions, pivot, category, palette version, and
  SHA-256.
- `docs/standards/asset-provenance.md` defines the required fields.
- Do not edit generated PNGs by hand. Change the generator, regenerate, and
  commit the output.
- Do not add attribution for tools in commits, code, documentation, or UI. The
  project identity is the author of the generated output.
