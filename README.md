# JabaSurvivol

JabaSurvivol is a 2D top-down, round-based zombie survival game for Android phones and tablets. It is built with Unity 6.3 LTS and a .NET 10 backend, with persistent progression, touch-first controls, and an autonomous engineering workflow.

## Status

Repository bootstrap is complete. The .NET solution, Aspire AppHost, service skeletons, agent definitions, standards, and automation are in place. The Unity project is created in the next phase after Unity 6.3 LTS is installed.

## Product rules

- Single-player, Android-first, landscape, touch controls.
- One character.
- Level cap 30, 35 skill points, free respec between runs.
- Persistent economy: supplies, XP, skill points, weapons, upgrades, and armor survive death.
- Death resets only the current run.
- Premium currency is cosmetic-only and implemented last.
- Localization is implemented only in the final phase before monetization.
- English is the only repository and product language until localization.

## Architecture

```text
Unity Android/Windows client
            |
            v
   BFF / API gateway
    /       |        \
Identity   Game   Telemetry worker
    \       |        /
     PostgreSQL / RabbitMQ
```

The game client is authoritative only for presentation. Multiplayer does not exist in the MVP. Premium wallet and leaderboard-sensitive data will be server-authoritative when introduced.

## Repository layout

```text
.github/          GitHub workflows, issue forms, PR template
.opencode/        Persistent agents, commands, and skills
src/              .NET 10 solution, services, shared core
tests/            Unit, architecture, integration, smoke tests
unity/            Unity project and local UPM packages
deploy/           Kubernetes, observability, secrets, scripts
infra/            Oracle and infrastructure bootstrap
docs/             Standards, ADRs, design, setup, playtests
tools/            Agent, bootstrap, and build scripts
```

## Prerequisites

- .NET SDK 10.0.401 or later patch of the 10.0.4xx band.
- Git and Git LFS.
- Unity Hub and Unity 6.3 LTS with Android and Windows build support.
- Android SDK, NDK, OpenJDK, and a physical Android device or tablet for playtesting.
- Docker Desktop for local containers.
- GitHub CLI for PR automation.

## Quick start

```powershell
dotnet restore JabaSurvivol.slnx
dotnet build JabaSurvivol.slnx -c Release
dotnet test JabaSurvivol.slnx -c Release
dotnet run --project src/AppHost
```

Unity setup instructions are in `docs/setup/unity.md`.

## Documentation

- `AGENTS.md`: operating rules for autonomous agents.
- `docs/standards/`: English-only, commits, PRs, autonomy, security, localization.
- `docs/game-design/`: core loop, economy, skills.
- `docs/architecture/adr/`: architecture decisions.
- `docs/setup/`: GitHub, Unity, Oracle.
- `docs/agents/`: autonomy policy, review checklist, incident runbook, kill switch.

## License

Copyright (c) 2026 Javier Baena. All rights reserved. The license is provisional and may change before distribution.
