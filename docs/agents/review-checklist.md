# PR Review Checklist

## Scope

- [ ] The PR matches the linked issue and acceptance criteria.
- [ ] No unrelated changes.
- [ ] No premature features or scope creep.
- [ ] No overengineering.

## Language and policy

- [ ] English-only repository content; approved locale tables excepted.
- [ ] No AI, assistant, LLM, tool, or generation attribution.
- [ ] No tool co-author trailers.
- [ ] No false manual-testing claims.
- [ ] Conventional PR title.
- [ ] Completed PR template.

## Architecture

- [ ] `Game.Core` has no `UnityEngine` dependency.
- [ ] Boundaries and assembly definitions are respected.
- [ ] Contracts are versioned and backward-compatible.
- [ ] No new service or framework without an ADR.
- [ ] The BFF remains thin.

## Gameplay and economy

- [ ] Persistent progression is preserved on death.
- [ ] Level 30, 35 skill points, free respec.
- [ ] Turret is the survival active skill.
- [ ] Rewards and costs are data-driven.
- [ ] Caps and balance rules are respected.
- [ ] No pay-to-win, loot boxes, or randomized paid rewards.

## Unity

- [ ] Every `.meta` file is present.
- [ ] No generated project files or build output.
- [ ] Touch controls, safe area, phone/tablet, landscape.
- [ ] Object pooling in hot paths.
- [ ] No allocations in `Update` or physics callbacks.
- [ ] EditMode and PlayMode tests updated.
- [ ] APK builds.
- [ ] `logcat` has no fatal errors.

## Backend

- [ ] Build and tests pass.
- [ ] Migrations are additive and safe.
- [ ] Health checks and telemetry exist.
- [ ] ProblemDetails and validation are present.
- [ ] No secrets in configuration.
- [ ] No destructive operations.

## Security

- [ ] No secrets or credentials.
- [ ] Authorization is enforced server-side.
- [ ] Premium wallet and leaderboard data are server-authoritative.
- [ ] Dependencies and licenses are acceptable.
- [ ] No untrusted scripts or unsafe network calls.

## Verification

- [ ] Build commands and results are included.
- [ ] Test commands and results are included.
- [ ] APK artifact is included for Android-visible changes.
- [ ] Screenshots or video are included for visual changes.
- [ ] Performance evidence is included for performance-sensitive changes.
- [ ] Save migration evidence is included for save changes.

## Verdict

- [ ] APPROVE
- [ ] COMMENT
- [ ] REQUEST_CHANGES
