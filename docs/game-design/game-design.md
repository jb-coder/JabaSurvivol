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
- Four weapons.
- Three zombie types plus one boss.
- Ten rounds.
- Armory between rounds.
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
- Elite enemy every five rounds.
- Boss at round 10.
- Endless mode is considered after the MVP.
- Round completion grants supplies and a choice of one of three rewards.

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
