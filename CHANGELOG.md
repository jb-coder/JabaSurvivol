# Changelog

All notable changes to this project are documented in this file.

The format is based on Keep a Changelog and the project follows Semantic Versioning.

## [Unreleased]

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
