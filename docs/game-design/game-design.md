# JabaSurvivol Game Design

## Pitch

JabaSurvivol is a 2D top-down, round-based zombie survival game for Android phones and tablets. The player controls a single survivor, fights escalating zombie waves, earns persistent supplies and XP, and builds a character across runs.

## Core loop

1. Start a run in the arena.
2. Survive a zombie round.
3. Earn supplies and XP.
4. Choose one of three round rewards.
5. Spend supplies on weapons, weapon upgrades, and armor.
6. Spend skill points on the character tree.
7. Start the next round.
8. Reach round 10 and defeat the boss.

## MVP scope

- One arena.
- One playable character.
- Five weapons.
- Three zombie types plus one boss.
- Ten rounds.
- Armory from the main menu and pause menu.
- Persistent supplies, XP, levels, skills, weapons, and armor.
- Local save.
- Android APK builds for verification.
- No co-op, payments, localization, or premium currency in the MVP.

## Controls

- Landscape orientation.
- Left virtual stick: movement.
- Right virtual stick: aim and fire.
- Buttons: reload, weapon switch, active skill, pause.
- Options: auto-fire, aim assist, left-handed layout, joystick size and opacity.
- Safe-area support for phones and tablets.

## Rounds

- Round spawn budget increases with the round number.
- Zombie health, speed, and damage scale with the round.
- The spawn composition is deterministic from the run seed and round number.
- Walker spawns from round 1, Runner from round 2, and Brute from round 4.
- Elite enemy every five rounds, including round 5.
- Boss at round 10.
- Endless mode is considered after the MVP.
- Round completion grants supplies and a choice of one of three rewards.

### Zombie archetypes

| Archetype | Health | Speed | Contact damage | Supplies | XP | Visual scale |
|---|---:|---:|---:|---:|---:|---:|
| Walker | 1.00x | 1.00x | 1.00x | 10 | 10 | 1.00 |
| Runner | 0.70x | 1.70x | 0.80x | 15 | 15 | 0.82 |
| Brute | 2.60x | 0.70x | 1.60x | 30 | 30 | 1.35 |

Round scaling applies to the base round values before the archetype multipliers. The spawn plan guarantees at least one Runner in round 2 and later, and at least one Brute in round 4 and later, so mixed compositions appear as soon as the archetype unlocks.

### Elite rounds

Elite rounds occur every fifth round, including round 5, and exclude the round 10 boss. Every enemy in an elite round uses:

- 1.50x health.
- 1.25x contact damage.
- 2.00x supplies and XP.

The HUD shows the elite modifier for the whole round. Elite state is run-only and does not alter persistent progression rules.

### Round 10 boss

The Warden spawns only in round 10, after every regular enemy in the round has been killed. The round stays active until the boss dies.

| Value | Value |
|---|---:|
| Health | 2,400 |
| Move speed | 1.15 |
| Contact damage | 30 every 0.8 seconds |
| Slam damage | 45 |
| Slam radius | 2.6 |
| Slam telegraph | 0.9 seconds |
| Slam interval | 4.5 seconds |
| Enrage threshold | 50% health |
| Enraged slam interval | 0.6x normal |
| Supplies | 1,000 |
| XP | 500 |

The slam is telegraphed with an expanding red ring before it resolves. The boss enrages below half health and slams more often. Defeating the boss awards its supplies and XP once, records round 10 as the highest round, and enters the victory state.

The victory screen shows the round, kills, supplies earned, and XP earned, with next-run and main-menu actions. Death during the boss round preserves all persistent progression and resets only the current run.

## Rewards

- Supplies.
- XP.
- Upgrade token.
- Ammo crate.
- Armor plate.

Ammo and armor repairs are free or drop-based. They do not consume persistent currency.

## Persistence

Death resets only the current run:

- Health.
- Current round.
- Position.
- Enemies.

Everything else persists:

- Supplies.
- XP.
- Character level.
- Skill points.
- Allocated skills.
- Weapon unlocks and upgrades.
- Armor unlocks.
- Premium currency, when introduced.

## Setting and art

- Neon-lit urban survival.
- Readable silhouettes and a small palette.
- Pixel art or vector art with consistent rules.
- Every asset must be recorded in `THIRD_PARTY_NOTICES.md`.

## Non-goals

- Multiplayer.
- Open world.
- Procedural narrative.
- Loot boxes.
- Pay-to-win.
- Real-money features before the monetization phase.
- Localization before the final phase.
