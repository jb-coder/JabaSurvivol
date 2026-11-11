# Changelog

All notable changes to this project are documented in this file.

The format is based on Keep a Changelog and the project follows Semantic Versioning.

## [Unreleased]

### Added

- Username/password identity accounts with case-insensitive unique usernames, ASP.NET Core Identity password hashing, 15-minute JWT access tokens, and 30-day rotating refresh tokens stored as hashes.
- Registration, login, refresh, logout, and current-account endpoints under `/api/v1/identity`, with strict per-IP rate limiting and ProblemDetails responses.
- Reusable PostgreSQL Testcontainers fixture and identity integration tests that cover rotation, reuse revocation, logout, expiry, cross-account isolation, and rate limiting.
- Revision-safe primary cloud save endpoints with ETag support, 1 MiB limits, schema validation, and `409` conflict responses that never overwrite newer data.
- Game module PostgreSQL schema with an additive cloud-save migration and a unique account/slot constraint.
- Highest-round leaderboard with idempotent run submission, bounds validation, best run per account, top-50 default query capped at 100, deterministic tiebreaks, and per-account submission rate limiting.
- Public read-only remote config with minimum client version, operational flags, sample rate, ETag support, conservative cache headers, and startup validation that rejects invalid values.
- Authenticated telemetry ingestion with 1-100 event batches, 64 KiB request and 4 KiB payload limits, typed allowlists, timestamp windows, per-account idempotency, and no PII.
- Telemetry worker aggregation into idempotent daily totals with 30-day raw-event retention.
- BFF public boundary with YARP routing, JWT validation, per-IP and per-account rate limits, correlation IDs, safe ProblemDetails on authentication and upstream failures, and no public route to internal status endpoints.
- Unity account panel with register, login, logout, loading, signed-out, signed-in, and offline states, plus ProblemDetails handling and one refresh-and-retry on `401`.
- Access tokens kept in memory only; refresh tokens encrypted with Windows DPAPI or the Android Keystore and never written to `PlayerPrefs`.
- Android INTERNET permission enabled, with cleartext HTTP limited to development emulator builds.
- Unity cloud-save backup and restore with explicit revision conflicts, `409` handling, bounded upload retries, offline fallback, and sync at login, run end, and app pause.
- Cloud save revision persisted additively in schema version 1 so older profiles remain compatible.
- Unity highest-round leaderboard panel from the main menu and victory screen, with run submission, idempotent retries, and non-blocking failures.
- Opt-in telemetry with a persistent toggle, allowlisted event queue, batched retries, and remote-config gating that defaults to off.
- Hardened multi-stage backend container images and Kustomize manifests for Identity, Game, Telemetry Worker, BFF, and PostgreSQL, with non-root containers, probes, resource budgets, network policies, and BFF-only ingress.
- Container CI builds all four images, validates the rendered manifests, and scans images with Trivy.
- Gated ARM64 deploy workflow with immutable image digests, pre-migration backup, ordered migration Jobs, rollout probes, BFF smoke tests, and automatic rollback.
- Identity and Game migration-only startup mode for Kubernetes migration Jobs.

### Fixed

- Local Aspire AppHost now waits for PostgreSQL and upstream services before starting dependent projects, so the identity, game, telemetry, and BFF resources stay running during local verification.
- The profile store recovers persistent progression from the backup file when the main save is empty or unreadable, instead of returning a fresh profile.
- A pending cloud-save conflict is cleared when the signed-in account changes, so one account can never apply another account's cloud document.
- A local persistence failure during cloud backup or restore is reported as offline and never clears a pending conflict.

## [0.4.0] - 2026-11-08

### Added

- Data-driven zombie catalog in `Game.Core` for Walker, Runner, and Brute with health, speed, contact damage, supplies, XP, and visual scale values.
- Deterministic round spawn composition generated from the run seed and round number, with Runners from round 2 and Brutes from round 4.
- Elite rounds every fifth round with increased health, contact damage, and rewards, plus a HUD modifier.
- Archetype-based kill rewards and a pooled zombie actor and death-effect system in Unity.
- Round 10 boss encounter with a dedicated boss catalog, telegraphed slam attack, enrage phase, and boss HUD.
- Boss spawn gate that keeps the final round active until the boss is defeated.
- Victory state with run summary, next-run action, and main-menu action.
- Boss death rewards, highest-round recording, and persistent-progression-safe death during the boss round.
- Run-only weapon loadout with per-weapon magazines, finite reserves, reload timing, and weapon switching.
- Manual and automatic reload with effective reload time from weapon upgrades and Engineering.
- HUD weapon, ammo, reserve, and reload state, plus touch and keyboard reload and switch controls.
- Ammo crates refill every finite reserve without spending persistent supplies.
- Turret active skill with rank gating, deploy duration, target acquisition, rank-scaled damage, and cooldown.
- Active-skill HUD button with radial cooldown and Space keyboard fallback.
- Run-only active-skill state that resets on death and new runs.
- Dash active skill with rank-scaled distance, cooldown, and a brief invulnerability window.
- Shockwave active skill with rank-scaled area damage, radius, cooldown, and zombie knockback.
- Active-skill selection from the armory or character sheet with persistent profile storage and zero-rank gating.
- Berserker, Ghost, and Bastion synergies derived from allocated ranks without extra skill points.
- Character-sheet synergy status with active and requirement display.
- Balance simulation coverage for Walker, Runner, Brute, elite, and boss content.
- Balance simulation coverage for magazines, reserves, reloads, weapon switching, and ammo crate rewards.
- Balance simulation coverage for Turret, Dash, Shockwave, and synergy uptime, with boss time-to-kill and new outliers.

### Changed

- Android version bumped to `0.4.0` with version code `4`.
- Core package version bumped to `0.4.0`.

## [0.3.0] - 2026-11-07

### Added

- Data-driven weapon catalog in `Game.Core` for the pistol, SMG, shotgun, rifle, and LMG, including damage, fire rate, magazine size, reload time, range, reserve ammunition, and unlock cost.
- Persistent weapon unlocks and equipped weapon in the profile, with the pistol always available and infinite reserve.
- Armory panel accessible from the main menu and pause menu with supply purchases, equip actions, and touch-friendly weapon rows.
- Combat now uses the equipped weapon's damage, fire interval, range, projectile speed, pellet count, and spread.
- Permanent weapon upgrade tiers for damage, fire rate, magazine size, and reload speed, with five tiers per stat, per-weapon costs, and effective stat aggregation.
- Armory upgrade section showing the equipped weapon's current tier, next cost, next effect, and max-tier state, with atomic purchases.
- Profile persistence for weapon upgrades that survives death and remains compatible with schema version 1 saves.
- Data-driven armor tiers for light, medium, and heavy armor with absorb amounts, unlock costs, and five permanent upgrade tiers per armor.
- Run armor now absorbs damage before health, resets to its maximum on a new run, and is repaired by free armor plate drops.
- Armory armor tab with unlock, equip, and upgrade actions, plus a HUD armor readout.
- Profile persistence for armor unlocks, equipped armor, and armor upgrades under schema version 1.
- Data-driven nine-node Strength, Agility, and Survival skill tree with five ranks per node and Turret rank data.
- Skill point allocation, free respec, active skill slot persistence, and the 35-point level 30 progression rule.
- Deterministic stat aggregation across base stats, skills, weapon upgrades, armor, and temporary modifiers, with movement speed, damage, armor, critical chance, and headshot caps.
- Combat, movement, health, and armor now consume the aggregated character stats.
- Character sheet with phone branch tabs, tablet three-column layout, rank pips, large touch targets, and before/after stat previews.
- Character sheet access from the main menu, pause menu, and round-end screen, with free respec between runs.
- HUD level, XP progress, available skill points, and a temporary level-up notification.
- Extended headless balance simulation for the full v0.3.0 build path, including rifle unlock, weapon upgrade tiers, armor tiers, skill ranks, supplies earned/spent/remaining, XP, level, and available skill points.
- Simulation outliers for trivial rounds, runaway supplies, unreachable upgrades, cap violations, and missing weapon, damage upgrade, or armor unlocks.

### Changed

- Android version bumped to `0.3.0` with version code `3`.
- Core package version bumped to `0.3.0`.

## [0.2.0] - 2026-11-06

### Added

- Deterministic round lifecycle in `Game.Core` with round start, active, cleared, and failed phases, per-round spawn budgets, and round-scaled zombie health, speed, and damage.
- HUD round counter with enemies remaining and the persistent highest round record.
- Deterministic round reward options for supplies, XP, ammo crates, and armor plates, with persistent and run-only application rules.
- Safe-area-aware round-end reward selection panel with three touch cards, keyboard shortcuts, saved selection, and automatic next-round start.
- Headless deterministic balance simulation for rounds 1 to 10 with pacing, economy growth, and outlier reporting.

### Changed

- `GameSession` now uses the round model instead of a fixed kill count; clearing a round pauses for the reward flow before the next round starts.
- Android version bumped to `0.2.0` with version code `2`.

## [0.1.0] - 2026-11-06

### Added

- Initial repository structure for Unity, .NET, infrastructure, documentation, and agents.
- .NET 10 solution with Aspire AppHost, service defaults, service skeletons, and tests.
- Autonomous agent definitions, commands, and skills.
- Repository standards for English-only content, commits, pull requests, autonomy, security, localization, and asset provenance.
- Game design documentation for the core loop, persistent economy, and skill tree.
- GitHub CI, PR policy, issue forms, and release automation scaffolding.
- GitHub Project `JabaSurvivol Roadmap` with fields, milestones, views, and automated item synchronization.
- Unity 6.3 Universal 2D project with URP 2D rendering, landscape configuration, and Android, Windows IL2CPP, and WebGL build targets.
- Local UPM package `com.jabasurvivol.core` backed by the shared `src/Game.Core` source.
- Walking skeleton: main menu, twin-stick touch controls, keyboard and mouse fallback, movement, shooting, zombie, health, supplies, XP, rounds, pause, game over, restart, and safe-area-aware phone and tablet layouts.
- Atomic local profile persistence with versioned validation and corrupted-save recovery.
- EditMode and PlayMode tests plus Android ARM64 and emulator build automation.

### Changed

- GitHub Actions dependencies updated to their current major versions.
- .NET package group updated to the validated dependency set.
- Dependabot now targets `develop` and uses a Conventional Commits prefix.

### Fixed

- HUD text and fill updates now avoid per-frame string formatting.
- Profile saves preserve the previous file as a backup and recover from a corrupt main file.
- Project configuration no longer recreates the committed scene on every build.
