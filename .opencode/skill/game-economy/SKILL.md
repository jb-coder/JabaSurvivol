---
name: game-economy
description: Use when designing, implementing, or balancing JabaSurvivol progression, supplies, XP, skill points, skills, weapons, armor, rounds, rewards, and premium currency.
---

# JabaSurvivol Game Economy

## Core rule

Progression is persistent. Death resets only the current run: health, round, position, and enemies.

Never remove on death:

- Supplies.
- XP.
- Character level.
- Skill points.
- Allocated skills.
- Weapon unlocks and upgrades.
- Armor unlocks.

Donations are not progression and grant no resources. Premium currency is
deferred (ADR-0008).

## Currencies and progression

| Resource | Earned from | Spent on | Lost on death |
|---|---|---|---|
| Supplies | Kills, headshots, rounds, elites, boss | Weapons, upgrades, armor | No |
| XP | Kills, rounds, challenges, boss | Character levels | No |
| Skill points | Character levels | Skill tree | No |

Cores are not implemented. Optional donations grant no resources.

## Character progression

- Level cap: 30.
- 1 skill point per level.
- 1 extra point at levels 5, 10, 15, 20, 25, and 30.
- Total: 35 points.
- Full tree cost: 45 points.
- Free respec between runs during the MVP.
- Allocate points only in the armory, pause menu, or round-end screen.

## Skill tree

- Strength: +3% weapon damage per rank, max 5.
- Vitality: +10 max health per rank, max 5.
- Agility/Speed: +2% movement speed per rank, max 5.
- Precision: +1% critical chance and +5% headshot damage per rank, max 5.
- Endurance: +5 armor per rank, max 5.
- Engineering: +5% reload speed and +5% ammo capacity per rank, max 5.
- Turret: active survival skill, 5 ranks.

## Caps

- Movement speed bonus: +50%.
- Total damage bonus: +200%.
- Armor: 200.
- Critical chance: 50%.
- Headshot bonus: +100%.

## Rewards

After each round, offer one of three:

- Supplies.
- XP.
- Upgrade token.
- Ammo crate.
- Armor plate.

Ammo and armor repairs must not consume persistent currency.

## Donations and deferred premium currency

- An optional PayPal donation link may be shown.
- Donations grant no Supplies, XP, Cores, items, cosmetics, content, or
  advantage.
- Cores, the cosmetic store, battle pass, and payments are deferred.
- When introduced, premium currency must be cosmetic-only, server-authoritative,
  and free of loot boxes, randomized paid rewards, and pay-to-win (ADR-0008).

## Testing

- Unit test XP curves, skill point allocation, stat aggregation, caps, respec, and save migration.
- Run headless balance simulations to detect outliers.
- Keep all values data-driven and documented.
