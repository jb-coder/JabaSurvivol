# Character Progression and Skills

## Character level

- Level cap: 30.
- 1 skill point per level.
- 1 extra point at levels 5, 10, 15, 20, 25, and 30.
- Total: 35 skill points.
- Full tree cost: 45 points.
- Free respec between runs during the MVP.
- Points are allocated only in the armory, pause menu, or round-end screen.

## XP curve

| Level | XP to next | Cumulative points |
|---|---:|---:|
| 2 | 100 | 1 |
| 5 | 400 | 5 |
| 10 | 1,200 | 11 |
| 20 | 4,000 | 24 |
| 30 | 10,000 | 35 |

The curve is configurable and will be validated with simulations.

## Skill tree

### Strength

| Node | Effect | Ranks | Maximum |
|---|---|---:|---:|
| Strength | +3% weapon damage | 5 | +15% |
| Vitality | +10 max health | 5 | +50 HP |
| Shockwave | Area damage and knockback | 5 | More damage, radius, less cooldown |

### Agility

| Node | Effect | Ranks | Maximum |
|---|---|---:|---:|
| Speed | +2% movement speed | 5 | +10% |
| Precision | +1% critical chance, +5% headshot damage | 5 | +5% crit, +25% headshot |
| Dash | Short invulnerable dash | 5 | More distance, less cooldown |

### Survival

| Node | Effect | Ranks | Maximum |
|---|---|---:|---:|
| Endurance | +5 armor | 5 | +25 |
| Engineering | +5% reload speed, +5% ammo capacity | 5 | +25% each |
| Turret | Deployable turret | 5 | More duration and damage, less cooldown |

## Synergies

Delivered after the core loop is stable:

| Synergy | Requirement | Effect |
|---|---|---|
| Berserker | Strength 3 + Vitality 3 | +10% damage below 50% health |
| Ghost | Speed 3 + Precision 3 | +10% movement speed for 2 seconds after a kill |
| Bastion | Endurance 3 + Engineering 3 | Armor regenerates out of combat |

Synergies do not cost extra points.

## Stat calculation

```text
Final stats = base
            + skills
            + weapon upgrades
            + armor
            + temporary buffs
```

## Caps

- Movement speed bonus: +50%.
- Total damage bonus: +200%.
- Armor: 200.
- Critical chance: 50%.
- Headshot bonus: +100%.

## UI

- Character sheet accessible from the main menu, pause, and round end.
- Phone: three tabs, one per branch.
- Tablet: two or three columns.
- Large touch targets.
- Rank pips and long-press tooltips.
- Before/after stat comparison.
- Respec confirmation.
- HUD: level, XP bar, available points, and level-up notification.
- Active skill button with radial cooldown.

## Persistence

The profile stores level, XP, available points, allocated ranks, equipped active skill, supplies, weapon unlocks and upgrades, armor tier, records, settings, and schema version.

Saving occurs on round end, purchase, level-up, pause, and app exit. Writes are atomic and validated on load.

## Testing

- XP curve and level-up points.
- Skill point allocation and respec.
- Stat aggregation and caps.
- Save/load and migration.
- PlayMode verification of applied stats and active skills.
- Headless balance simulations for build outliers.
