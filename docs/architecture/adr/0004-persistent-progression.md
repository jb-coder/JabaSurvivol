# ADR-0004: Persistent Progression

## Status

Accepted.

## Context

The game is a round-based zombie survival experience for mobile. The user wants players to keep improving and advancing across runs. Losing upgrade currency on death would conflict with that goal.

## Decision

All progression is persistent:

- Supplies.
- XP.
- Character level.
- Skill points.
- Allocated skills.
- Weapon unlocks and upgrades.
- Armor unlocks.
- Premium currency, when introduced.

Death resets only the current run: health, round, position, and enemies.

Ammo and armor repairs are free or drop-based so players do not spend persistent currency on consumables.

## Alternatives considered

- Classic run-based economy: resets points on death; rejected by the user.
- Hybrid temporary and persistent currencies: adds confusion and a failure feeling.
- Pay-to-win acceleration: rejected for product and store-compliance reasons.

## Consequences

- The game is a meta-progression experience, not a pure run-based score chase.
- Difficulty and costs must scale to avoid trivializing the game.
- Balance simulations and telemetry become important.
- The save schema must be versioned and migrated safely.
