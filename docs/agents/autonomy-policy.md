# Autonomous Delivery Policy

## Operating principle

Agents own the full development loop: plan, implement, test, review, merge, build, deploy, verify, and document. Human involvement is limited to one-time account provisioning, legal acceptance, payments, and optional playtesting.

## Agent roles

| Agent | Mode | Purpose |
|---|---|---|
| `orchestrator` | primary | Plans, delegates, and enforces gates |
| `game-designer` | subagent | Economy, skills, rounds, balance |
| `architect` | subagent | ADRs, boundaries, contracts |
| `unity-dev` | subagent | Unity gameplay, touch UI, Android |
| `backend` | subagent | APIs, persistence, auth, telemetry |
| `tester` | subagent | Unit, PlayMode, integration, simulations |
| `pr-reviewer` | subagent | Independent review and veto |
| `security` | subagent | Security, secrets, payments, supply chain |
| `devops` | subagent | CI/CD, VPS handoff, releases, observability |
| `verifier` | subagent | Acceptance, builds, smoke tests |

## Separation of duties

- The builder never approves its own PR.
- The reviewer never edits code.
- The verifier never edits code.
- The security reviewer never edits code.
- The deployer does not change application code.

## Credentials

- Builder operations use `pwsh tools/agent/gh-builder.ps1` with the current `gh` login or `JABA_BUILDER_TOKEN`.
- Reviewer operations use `pwsh tools/agent/gh-reviewer.ps1`. The wrapper mints a short-lived installation token from the `jaba-survivol-reviewer-jb` GitHub App.
- The reviewer app credentials live outside the repository at `%USERPROFILE%\.config\jaba-survivol\github-app`.
- Never use plain `gh` for writes.
- Never print, commit, or log tokens.

## Autonomy loop

1. Pick an issue from `Ready`.
2. Create a short-lived branch.
3. Implement the smallest vertical slice.
4. Run tests and build the APK.
5. Open a PR with evidence.
6. Request review.
7. Fix blocking feedback.
8. Auto-merge after approval and green checks.
9. Release or deploy when applicable.
10. Update the board and changelog.

## Hard stops

- Failing required checks.
- Missing reviewer approval.
- Kill switch enabled.
- Secret or license violation.
- Destructive migration without backup.
- P0 or P1 defect.
- Three failed review cycles.

## Escalation without humans

Create an issue with `agent:blocked`, include the failing evidence, and move to the next unblocked task. Do not bypass the gate.
