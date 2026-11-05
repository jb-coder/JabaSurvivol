# JabaSurvivol Agent Instructions

## Project

JabaSurvivol is a 2D top-down, round-based zombie survival game for Android phones and tablets, built with Unity 6.3 LTS and a .NET backend. The project is a portfolio-quality product with persistent progression, touch-first controls, and autonomous engineering workflows.

## Non-negotiable product rules

- Platform priority: Android phones and tablets, landscape orientation, touch input.
- Secondary platform: Windows, used for faster debugging and automated desktop tests.
- Genre: 2D top-down twin-stick survival.
- Mode: single-player only for the MVP.
- One playable character for the MVP.
- Character level cap: 30.
- Skill points: 1 per level, plus 1 extra at levels 5, 10, 15, 20, 25, and 30. Total: 35.
- Respec is free between runs during the MVP.
- Skill points are allocated only in the armory, pause menu, or round-end screen.
- Survival active skill: Turret.
- Synergies are delivered after the core loop is stable.
- All progression is persistent. Death only resets the current run: health, round, position, and enemies.
- Never remove Supplies, XP, character level, skill points, allocated skills, weapon unlocks, weapon upgrades, or armor unlocks on death.
- Ammo and armor repairs are free or drop-based. Do not consume persistent currency on consumables.
- Premium currency (Cores) is implemented last and is cosmetic-only unless a future ADR explicitly changes this.
- No loot boxes, randomized paid rewards, or pay-to-win mechanics.
- Localization is the final phase before monetization. English is the only locale until then.

## Language policy

Everything committed to this repository must be in English:

- Code, comments, XML docs, test names, logs, error messages.
- UI strings, localization keys, asset names, folder names.
- Commit messages, PR titles, PR descriptions, issue titles, issue bodies.
- Documentation, ADRs, runbooks, release notes.

The user may speak Spanish in conversation. The repository remains English-only.

## Attribution policy

- Never mention AI, assistants, LLMs, generation tools, or agent frameworks in commits, PRs, issues, documentation, release notes, or code comments.
- Never add `Co-authored-by` trailers for tools.
- Never claim manual human testing unless a human actually performed it. Use truthful verification language such as `Automated verification: unit tests, PlayMode tests, Android build`.
- Use the project identity configured for the repository. Do not invent a human approval or review.

## Architecture

```text
Android / Windows Unity client
              |
              v
     BFF / API gateway
      /       |        \
Identity     Game   Telemetry worker
      \       |        /
       PostgreSQL / RabbitMQ
```

- `Game.Core` is pure C# in `src/Game.Core` and must not reference `UnityEngine`.
- Unity consumes the same game rules through the local UPM package in `unity/JabaSurvivol/Packages/com.jabasurvivol.core` once the Unity project exists.
- Backend starts as a modular monolith (`Game.Api`) with modules for identity, game, and telemetry. Extract services only when a concrete need is documented in an ADR.
- The target deployment is Oracle Always Free plus k3s, with a 2 OCPU / 12 GB budget. Do not assume 4 OCPU / 24 GB.
- The BFF is introduced when there is more than one service or a clear aggregation/authentication need.
- Payments are implemented last. Premium wallet state is server-authoritative.

## Repository map

- `src/Game.Core`: deterministic game rules, economy, skills, stats, balance.
- `src/Shared/Contracts`: API and event contracts shared with the client.
- `src/Services`: ASP.NET Core services and workers.
- `src/Gateway/Bff`: API gateway and aggregation layer.
- `src/AppHost`: Aspire orchestration for local development.
- `src/ServiceDefaults`: telemetry, health checks, resilience, service discovery.
- `tests`: unit, architecture, integration, and smoke tests.
- `unity/JabaSurvivol`: Unity project. Do not hand-edit generated project files.
- `deploy`: Kubernetes, observability, encrypted secrets, scripts.
- `infra`: Oracle bootstrap and infrastructure scripts.
- `docs`: standards, ADRs, game design, setup, playtests, releases.
- `.opencode`: persistent agents, commands, and skills.
- `.github`: workflows, templates, and automation.

## Commands

```powershell
dotnet restore JabaSurvivol.slnx
dotnet build JabaSurvivol.slnx -c Release
dotnet test JabaSurvivol.slnx -c Release
dotnet format JabaSurvivol.slnx --verify-no-changes
dotnet run --project src/AppHost
pwsh tools/agent/check-commit-style.ps1 -Message "feat(game): example"
pwsh tools/agent/check-forbidden-attribution.ps1 -Text "example text"
```

Unity commands are added after the Unity project is created. See `docs/setup/unity.md`.

## Git workflow

Gitflow with short-lived branches:

- `main`: production and release-ready. No direct pushes.
- `develop`: integration branch. No direct pushes.
- `feature/*`: from `develop`, PR to `develop`, squash merge.
- `bugfix/*`: from `develop`, PR to `develop` or a release branch, squash merge.
- `chore/*`, `docs/*`, `test/*`: from `develop`, PR to `develop`, squash merge.
- `release/*`: from `develop`, PR to `main`, merge commit, tag, backmerge to `develop`.
- `hotfix/*`: from `main`, PR to `main`, merge commit, tag, backmerge to `develop`.
- `backmerge/*`: from `main`, PR to `develop`, merge commit.

Every change enters through a pull request. No exceptions for documentation, CI, or hotfixes.

## Pull requests

- Title uses Conventional Commits: `feat(game): ...`, `fix(api): ...`, `docs(adr): ...`.
- Link the related issue.
- Use the PR template.
- Keep PRs small: one feature, bug, or chore per PR.
- Include verification evidence: tests, build artifact, screenshots or video for gameplay/UI changes.
- Include an APK link or artifact for Android-visible changes.
- No secrets, keystores, credentials, or generated build output.
- The reviewer agent must approve before merge. The reviewer can request changes and block the PR.
- Auto-merge is enabled only after the reviewer approves and all required checks pass.

## Autonomous review

- The repository owner account `jb-coder` is the builder. It creates branches and PRs.
- The GitHub App `jaba-survivol-reviewer-jb` is the independent reviewer. It approves or requests changes.
- Use `pwsh tools/agent/gh-builder.ps1` for builder GitHub operations.
- Use `pwsh tools/agent/gh-reviewer.ps1` for reviewer GitHub operations.
- The builder wrapper uses the current `gh` login or `JABA_BUILDER_TOKEN`. The reviewer wrapper mints an app installation token.
- Never use plain `gh` for writes and never print tokens.
- The builder never approves its own PR.
- The reviewer never edits code.
- Reviewers may reject up to three cycles. After three failed cycles, label the PR `agent:blocked`, open a follow-up issue, and move on.
- The reviewer checks acceptance criteria, architecture, tests, security, performance, English-only, and attribution policy.
- Required checks: `pr-policy`, `backend-ci`, `unity-tests`, `android-build`, `security`.
- The reviewer must not merge code it authored.

## Autonomy guardrails

- Work only inside this repository and the approved temp directory.
- Never modify personal files, account settings, billing, organization settings, or security settings.
- Never create paid cloud resources.
- Never print, commit, or exfiltrate secrets.
- Use least-privilege tokens and short-lived credentials where possible.
- Respect `AGENT_PAUSED=true`, `do-not-merge`, and `agent:paused`.
- Respect daily limits for PRs, review cycles, deploys, and CI minutes.
- Do not force-push protected branches.
- Do not delete namespaces, databases, or volumes without an approved backup and runbook.
- Destructive migrations are forbidden. Use additive, backward-compatible migrations.
- If a gate fails, fix the cause or park the task. Do not bypass the gate.

## Definition of done

A task is done only when:

- Acceptance criteria are met.
- English-only policy is respected.
- No AI attribution is present.
- Unit, architecture, and relevant integration tests pass.
- The solution builds in Release.
- Android APK builds for Android-visible changes.
- The reviewer approves and all required checks pass.
- Documentation and changelog are updated when behavior changes.
- There are no new P0 or P1 defects.
- The PR is merged through the approved workflow.

## Unity rules

- Unity 6.3 LTS, Universal 2D, URP 2D Renderer.
- Android: IL2CPP, ARM64, target API 36, minimum API 26.
- Use Assembly Definitions. Keep `Game.Core` free of `UnityEngine`.
- Commit every `.meta` file. Never commit `Library`, `Temp`, `Builds`, or generated `.csproj`/`.sln` files.
- Use object pooling for zombies, projectiles, VFX, and floating text.
- Avoid allocations in `Update`, `FixedUpdate`, and hot paths.
- Use the Input System and touch controls. Test phone and tablet layouts, safe areas, and landscape orientation.
- User-facing strings must use localization keys from day one. Only English is active until the localization phase.
- Profile on a real Android device before declaring performance work complete.

## Backend rules

- .NET 10 LTS, ASP.NET Core minimal APIs, nullable enabled.
- PostgreSQL with separate schemas/databases per bounded context.
- Prefer additive migrations. Back up before migrating.
- Health checks: `/health` and `/alive`.
- OpenTelemetry for logs, metrics, and traces.
- ProblemDetails for API errors.
- No secrets in configuration files. Use environment variables or a secret store.
- API contracts are versioned under `/api/v1`.
- The BFF is the only public HTTP service in production.

## Testing

- `Game.Core` tests use xUnit and must be fast.
- Unity EditMode tests cover data/config and save logic.
- Unity PlayMode tests cover input, combat, rounds, economy, and skills.
- Integration tests use Testcontainers when persistence is introduced.
- Architecture tests enforce project boundaries and the no-UnityEngine rule in `Game.Core`.
- Balance simulations run headlessly and report outliers.
- Automated verification is not a substitute for human playtesting, but human playtesting is optional and non-blocking unless the user explicitly requests it.

## References

- `docs/standards/english-only.md`
- `docs/standards/commits.md`
- `docs/standards/pull-requests.md`
- `docs/standards/autonomy.md`
- `docs/standards/security.md`
- `docs/standards/localization.md`
- `docs/agents/autonomy-policy.md`
- `docs/agents/review-checklist.md`
- `docs/agents/kill-switch.md`
- `docs/game-design/game-design.md`
- `docs/game-design/economy.md`
- `docs/game-design/skills.md`
