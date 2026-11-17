# Touch Controls and Accessibility

## Status

Proposed for v0.9.0. This note defines the defaults, ranges, and
accessibility intent for the touch and settings work. Implementation follows
the milestones and issues that link to this note.

## Scope

The note covers:

- Auto-fire behavior.
- Aim assist.
- Left-handed layout.
- Joystick size and opacity.
- Reduced motion.
- Haptics.
- Colorblind-safe combat telegraphs.
- Settings storage and reset behavior.

It does not add content, balance changes, new locales, or new input devices.

## Settings storage

All v0.9.0 settings are device-local. They use the existing `PlayerPrefs`
pattern and do not change the `Profile` schema or the cloud save document.

- Defaults apply when a key is absent or invalid.
- Values are clamped on load and on write.
- The settings panel can reset every v0.9.0 setting to its default.
- Audio and language remain device-local, matching the current behavior.

## Defaults and ranges

| Setting | Default | Valid values | Step |
|---|---|---|---|
| Left-handed layout | Off | Off, On | N/A |
| Joystick size | 1.00 | 0.80 to 1.40 | 0.10 |
| Joystick opacity | 1.00 | 0.40 to 1.00 | 0.10 |
| Aim assist | Off | Off, Low, High | N/A |
| Auto-fire | On | Off, On | N/A |
| Reduced motion | Off | Off, On | N/A |
| Haptics | Off | Off, On | N/A |
| Colorblind-safe telegraphs | Off | Off, On | N/A |

The defaults preserve the current behavior where possible. Opt-in settings
stay off unless the player enables them.

## Auto-fire

The current touch model fires while the aim stick is engaged. That behavior
stays the default and is exposed as the auto-fire setting.

- **On**: the weapon fires continuously while the aim stick is engaged.
- **Off**: the aim stick only aims. Firing uses a dedicated fire control
  delivered by the settings milestone that implements the control options.

Auto-fire never changes damage, fire rate, reload time, or accuracy. It only
changes whether the player must press a separate fire control.

## Aim assist

Aim assist is optional and disabled by default. When enabled, it nudges the
aim direction toward the nearest valid enemy inside a bounded cone.

| Level | Cone | Blend | Maximum range |
|---|---:|---:|---:|
| Off | 0 degrees | 0.00 | 0 units |
| Low | 6 degrees | 0.35 | 10 units |
| High | 12 degrees | 0.65 | 12 units |

Rules:

- The target is the nearest living enemy inside the cone and range.
- The final aim direction blends the raw aim direction with the direction to
  the target by the level's blend factor.
- Aim assist falls back to the raw aim direction when no valid target exists.
- Aim assist never changes damage, fire rate, reload time, enemy behavior, or
  projectile speed.
- The calculation is deterministic for the same input, player position, and
  enemy positions.

## Left-handed layout

Left-handed mode mirrors the touch controls without mirroring text.

- The movement stick moves to the right and the aim stick moves to the left.
- Reload, weapon switch, active skill, pause, and the manual fire control move
  to the opposite side of the screen.
- HUD text, health bars, round counters, and menus keep their reading order.
- The controls respect the safe area and must not overlap the HUD, the boss
  panel, or each other.
- The layout keeps the same touch target sizes and spacing as the default
  layout.

## Joystick size and opacity

Joystick size scales the base ring, the knob, and the touch radius by the same
factor. The rest position stays inside the safe area.

Joystick opacity changes only the visual alpha of the base ring and knob. It
does not change the touch radius, dead zone, or hit target.

## Reduced motion

Reduced motion is an opt-in comfort setting.

When enabled:

- Bloom and vignette are disabled.
- Non-essential camera motion is disabled.
- Non-essential UI motion is reduced or removed.
- Gameplay-critical feedback remains visible, including damage feedback and
  boss telegraphs.

The existing low-end post-processing fallback remains independent of this
setting.

## Haptics

Haptics are opt-in and default to off.

When enabled, supported Android devices may use short, low-amplitude feedback
for:

- Player damage.
- Boss slam impact.
- Round clear.

Haptics never fire for continuous weapon fire, menu navigation, or background
events. When disabled, no haptic call is made.

## Colorblind-safe telegraphs

Colorblind-safe telegraphs are opt-in and default to off.

When enabled, area-attack telegraphs add a high-contrast outline or pattern in
addition to color. The boss slam keeps its current timing, radius, and damage.
Color is never the only cue for a damaging area.

## Test requirements

| Area | Required coverage |
|---|---|
| Settings storage | Defaults, load, save, clamp, reset, and restart persistence |
| Left-handed layout | Stick and action-button anchors, safe-area fit, phone and tablet |
| Joystick size | Visual and touch radius scale together |
| Joystick opacity | Visual alpha changes without changing the hit target |
| Aim assist | Off, Low, and High behavior and raw-aim fallback |
| Auto-fire | On and Off behavior |
| Reduced motion | Bloom and vignette disabled while gameplay remains readable |
| Haptics | No haptic call when off |
| Telegraphs | Colorblind-safe variant remains readable |
| Localization | Key parity, placeholder parity, and phone/tablet layout |

## Out of scope

- UI text scaling.
- Screen reader support.
- Controller support.
- New content, balance changes, or new locales.
- Profile-backed or cloud-synced settings.

## References

- `docs/game-design/game-design.md`
- `docs/standards/localization.md`
- `docs/standards/audio.md`
- `docs/architecture/adr/0009-localization-architecture.md`
