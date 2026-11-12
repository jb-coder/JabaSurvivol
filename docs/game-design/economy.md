# Economy

## Resources

| Resource | Earned from | Spent on | Lost on death |
|---|---|---|---|
| Supplies | Kills, headshots, rounds, elites, boss, challenges | Weapons, weapon upgrades, armor | No |
| XP | Kills, rounds, challenges, boss | Character levels | No |
| Skill points | Character levels | Skill tree | No |
| Rounds reached | Survival | Nothing; it is a record | Not applicable |

Cores are not implemented. Optional donations grant no resources, currency, or
items.

## Income

Initial balance targets:

| Source | Supplies | XP |
|---|---:|---:|
| Walker kill | 10 | 10 |
| Runner kill | 15 | 15 |
| Brute kill | 30 | 30 |
| Elite round kill | archetype value x2 | archetype value x2 |
| Boss kill | 1,000 | 500 |
| Round clear | 100 | 50 |
| Run end bonus | 100 + 25 per round | 100 + 25 per round |

All values are data-driven and subject to simulation.

## Weapons

| Weapon | Unlock cost | Role |
|---|---:|---|
| Pistol | 0 | Starting weapon, infinite reserve |
| SMG | 500 | Close-range sustained damage |
| Shotgun | 900 | Burst damage, short range |
| Rifle | 1,200 | Accurate mid-range damage |
| LMG | 2,000 | Sustained suppression |

`Game.Core` owns the data-driven catalog in `WeaponCatalog`. It is the single
source of truth for damage, fire rate, magazine size, reload time, range,
projectile speed, reserve ammunition, pellet count, spread, and unlock cost.
The pistol always starts unlocked and has infinite reserve.

Each run creates a `WeaponLoadout` with one runtime state per unlocked weapon.
The state stores the magazine, finite reserve, reload timer, and reload progress.
Firing consumes one round per trigger pull; the shotgun consumes one shell and
then resolves its pellet spread. Reload time is `reloadSeconds / reloadSpeedMultiplier`,
where the multiplier includes Engineering and weapon reload upgrades. Engineering
also scales magazine capacity through `AmmoCapacityMultiplier`. Ammo crates add
to every finite reserve and never touch persistent supplies. Ammo is run-only and
resets on death or a new run.

The armory is available from the main menu and the pause menu. It spends
persistent supplies to unlock weapons and equips owned weapons. Unlocks and the
equipped weapon are stored in the profile and survive death.

Weapon upgrades use supplies and are permanent. Each weapon has five tiers for
each stat:

- Damage: +10% base damage per tier.
- Fire rate: +8% shots per second per tier.
- Magazine size: +15% base capacity per tier.
- Reload speed: +10% reload speed per tier.

Costs are data-driven per weapon and stat and grow with each tier. The armory
shows the current tier, the next cost, and the next effect for the equipped
weapon. Max-tier purchases are rejected without spending supplies. Upgrades are
stored in the profile and survive death.

## Armor

- Light: 25 absorb, free unlock.
- Medium: 50 absorb, 800 supplies.
- Heavy: 90 absorb, 1,600 supplies.
- Armor absorbs damage before health.
- Armor absorbs whole points. Fractional damage below 1 is not absorbed, and
  any fractional remainder reaches health.
- Repairs are free or drop-based. An armor plate restores 10 condition and
  never consumes persistent supplies.
- Each unlocked armor tier has five permanent upgrade tiers at +10% absorb per
  tier, with data-driven costs.
- Unlocks, equipped armor, and upgrades are stored in the profile and survive
  death. Death resets only the current run's armor condition.
- Equipping or upgrading armor from the pause-menu armory applies to the next
  run; an active run keeps its starting armor tier and maximum condition.

## Round rewards

After each round, the player chooses one of three options generated
deterministically from the run seed and the round number. The pool contains:

- Supplies.
- XP.
- Ammo crate.
- Armor plate.

| Reward | Round 1 | Round 10 | Persistence |
|---|---:|---:|---|
| Supplies | 50 | 275 | Persistent profile |
| XP | 30 | 165 | Persistent profile |
| Ammo crate | 24 | 24 | Current run only |
| Armor plate | 10 | 10 | Current run only |

The base round-clear bonus is awarded separately: 100 supplies and 50 XP.
Ammo and armor are free, drop-based, and never purchased with persistent
supplies.

## Round pacing

Round values are data-driven in `GameBalance` and `RoundTable`:

- Spawn budget: `4 + 2 * round`, from 6 enemies at round 1 to 24 at round 10.
- Zombie health: `60 * (1 + 0.18 * (round - 1))`, from 60 to 157.2.
- Zombie speed: `1.9 * (1 + 0.04 * (round - 1))`, capped at 1.35x.
- Zombie contact damage: `10 * (1 + 0.12 * (round - 1))`, from 10 to 20.8.
- Round 5 is an elite round and round 10 is the boss round.
- The round 10 boss spawns after the regular enemies are cleared and the round does not clear until it dies.
- The boss has 2,400 health, 1.15 move speed, 30 contact damage every 0.8 seconds, and a telegraphed 45-damage slam with a 2.6 radius, 0.9-second telegraph, and 4.5-second interval.
- The boss enrages below 50% health and uses a 0.6x slam interval while enraged.
- Defeating the boss awards 1,000 supplies and 500 XP once and enters the victory state.
- Archetype multipliers: Walker 1.00x/1.00x/1.00x, Runner 0.70x health and 1.70x speed, Brute 2.60x health, 0.70x speed, and 1.60x contact damage.
- Elite rounds multiply health by 1.50x, contact damage by 1.25x, and supplies and XP by 2.00x.
- Spawn composition is deterministic from the run seed and round number, with Runners from round 2 and Brutes from round 4.

The headless simulation uses the same core rules and reports pacing, income,
progression, build state, enemy mix, ammunition pressure, active-skill uptime,
synergy uptime, boss time-to-kill, and outliers:

```powershell
dotnet run --project tools/BalanceSimulation/BalanceSimulation.csproj -c Release
```

The default build is deterministic: it unlocks light armor, saves for the
rifle, then medium armor, then buys rifle upgrades, armor upgrades, and skill
ranks in a fixed priority. The report includes per-round weapon, unlock flags,
weapon upgrade tiers, armor tier and absorb, skill ranks, supplies earned,
spent, and remaining, XP, level, and available skill points.

It writes `artifacts/balance/rounds.md` with a fixed seed so CI and local runs
produce the same report. Outliers cover trivial elites, boss encounters that
are too easy or too hard, ammo starvation, runaway supplies, unreachable
upgrades, unreachable synergies, cap violations, and builds that never unlock a
weapon or weapon damage upgrade.

## Donations (current phase)

- An optional PayPal donation link may be shown outside the game.
- Donations grant no Supplies, XP, Cores, items, cosmetics, content, or
  advantage.
- Donations are outside the game economy.

## Premium currency (deferred)

- Cores, the cosmetic store, the battle pass, and payments are deferred.
- When introduced, they must be cosmetic-only, server-authoritative, and free
  of loot boxes, randomized paid rewards, and pay-to-win mechanics (ADR-0008).

## Anti-inflation

- Costs scale with tier.
- Enemy difficulty scales faster than passive income.
- Level gates limit access to high-tier weapons.
- Balance simulations detect runaway economies.
- Telemetry tracks income, spend, and progression speed.

## Principles

- The player never loses persistent progress on death.
- The player always has a meaningful next upgrade.
- No resource should become useless in the late game.
- Every currency has one clear purpose.
