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
| 20 | 4,000 | 23 |
| 30 | 10,000 | 35 |

The curve is configurable and will be validated with simulations.

## Skill tree

The nine nodes are data-driven in `SkillCatalog`. Each node has five ranks.
Turret, Dash, and Shockwave activation are delivered in this milestone.

Shockwave, Dash, and Turret store per-rank activation values: area damage and
radius, dash distance, turret damage and duration, and cooldown reduction.

### Turret activation

- Turret is the selected active skill by default.
- Rank 1: 8 damage per shot, 8 second duration, 19 second cooldown.
- Rank 5: 16 damage per shot, 16 second duration, 15 second cooldown.
- The turret fires every 0.5 seconds at the nearest enemy within 8 units.
- One turret can be deployed at a time. It cannot be redeployed while active or
  while the cooldown is running.
- Turret state is run-only and resets on death or a new run.
- The HUD shows a touch active-skill button with a radial cooldown, plus a
  keyboard fallback on Space.

### Dash activation

- Rank 1: 3 unit distance, 5.5 second cooldown, 0.25 second invulnerability.
- Rank 5: 5 unit distance, 3.5 second cooldown, 0.25 second invulnerability.
- Dash moves in the movement direction, or the aim direction when the player is
  standing still.
- Damage is ignored while the dash invulnerability window is active.
- Dash state is run-only and resets on death or a new run.

### Shockwave activation

- Rank 1: 40 area damage, 2.0 radius, 9 second cooldown.
- Rank 5: 80 area damage, 3.0 radius, 5 second cooldown.
- Shockwave resolves instantly around the player, damages every enemy in range,
  and knocks surviving zombies away.
- Shockwave state is run-only and resets on death or a new run.

### Active skill selection

- The player selects one allocated active skill from the armory or character
  sheet.
- Zero-rank skills cannot be selected or activated.
- The selection persists in the profile and applies to the next activation.
- Selecting a different skill replaces the previous active skill.

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

Synergies are derived from the allocated ranks and are never stored as
purchases:

- Berserker activates while health is below 50% and adds 10% weapon damage.
- Ghost activates for 2 seconds after a kill and adds 10% movement speed.
- Bastion regenerates 2 armor per second after 5 seconds without taking damage.
- The character sheet shows active synergies and the requirement progress for
  inactive synergies.
- Synergy modifiers are passed explicitly into stat aggregation, so repeated
  aggregation does not double-apply them.

## Stat calculation

```text
Final stats = base
            + skills
            + weapon upgrades
            + armor
            + temporary buffs
```

`StatAggregation.Compute` receives the effective weapon stats (base plus
permanent upgrades) and effective armor stats (base plus permanent upgrades),
then adds skill ranks and optional temporary modifiers.

Final stats:
- Vitality adds 10 max health per rank.
- Strength adds 3% weapon damage per rank.
- Speed adds 2% movement speed per rank.
- Precision adds 1% critical chance and 5% headshot damage per rank.
- Endurance adds 5 armor per rank.
- Engineering adds 5% reload speed and 5% ammo capacity per rank.

## Caps

- Movement speed bonus: +50%.
- Total damage bonus: +200%.
- Armor: 200.
- Critical chance: 50%.
- Headshot bonus: +100%.

The damage cap applies to the final multiplier after weapon upgrades, skills,
and temporary bonuses are combined.

## UI

- Character sheet accessible from the main menu, pause, and round end.
- Phone: three tabs, one per branch.
- Tablet: three columns, one per branch.
- Large touch targets, rank pips, and before/after stat previews.
- Inline previews replace long-press tooltips in the MVP.
- Free respec between runs. Respec is a single tap in the MVP; a confirmation
  step is deferred.
- HUD: level, XP progress, available points, and level-up notification.
- Active skill button with radial cooldown.

## Persistence

The profile stores level, XP, available points, allocated ranks, equipped active skill, supplies, weapon unlocks and upgrades, armor tier, records, settings, and schema version.

Allocated ranks are stored as node and rank pairs. Respec clears the pairs and
refunds every spent point without changing level, XP, weapon unlocks, or armor
unlocks. Allocation is allowed only from the armory, pause menu, or round-end
screen.

Saving occurs on round end, purchase, level-up, pause, and app exit. Writes are atomic and validated on load.

## Testing

- XP curve and level-up points.
- Skill point allocation and respec.
- Stat aggregation and caps.
- Save/load and migration.
- PlayMode verification of applied stats and active skills.
- Headless balance simulations for build outliers.
