# Economy

## Resources

| Resource | Earned from | Spent on | Lost on death |
|---|---|---|---|
| Supplies | Kills, headshots, rounds, elites, boss, challenges | Weapons, weapon upgrades, armor | No |
| XP | Kills, rounds, challenges, boss | Character levels | No |
| Skill points | Character levels | Skill tree | No |
| Cores | Real money, battle pass, events | Cosmetics, battle pass, slots | No |
| Rounds reached | Survival | Nothing; it is a record | Not applicable |

## Income

Initial balance targets:

| Source | Supplies | XP |
|---|---:|---:|
| Walker kill | 10 | 10 |
| Runner kill | 15 | 15 |
| Brute kill | 30 | 30 |
| Elite kill | 250 | 100 |
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

Weapon upgrades use supplies and are permanent. Suggested tiers:

- Damage.
- Fire rate.
- Magazine size.
- Reload speed.

## Armor

- Light, medium, and heavy tiers.
- Absorbs damage before health.
- Repairs are free or drop-based.
- Upgrades are permanent.

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
| Armor plate | 1 | 1 | Current run only |

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

The headless simulation uses the same core rules and reports pacing, income,
progression, and outliers:

```powershell
dotnet run --project tools/BalanceSimulation/BalanceSimulation.csproj -c Release
```

It writes `artifacts/balance/rounds.md` with a fixed seed so CI and local runs
produce the same report. Outliers cover trivial rounds, runaway supplies, and
unreachable upgrade costs.

## Premium currency

- Cores are implemented last.
- Cosmetic-only by default.
- Server-authoritative wallet.
- No loot boxes.
- No randomized paid rewards.
- No pay-to-win.

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
