# Autonomy Standards

## Goal

Agents deliver, review, build, merge, and deploy without human review. Human involvement is limited to one-time account provisioning, legal acceptance, payments, and optional playtesting.

## Identities

- **Builder:** `jb-coder` creates branches, commits, PRs, enables auto-merge, and merges.
- **Reviewer:** the GitHub App `jaba-survivol-reviewer-jb` approves or requests changes.
- **Deployer:** optional short-lived credentials deploy backend images and releases.

The builder must not approve its own PR. The reviewer app must not edit code. The deployer must not change application code.

## Pre-merge gates

- Valid Gitflow source and target branches.
- Conventional PR title and linked issue.
- Completed PR template.
- Reviewer approval.
- All required checks green.
- No `do-not-merge`, `agent:blocked`, `needs-human`, or `agent:paused`.
- No secrets or forbidden attribution phrases.
- English-only content.
- Acceptance evidence.

## Limits

- Maximum 3 review cycles per PR.
- Maximum PR size: one vertical slice; split larger changes.
- Maximum daily deploys: 2 unless a hotfix is required.
- Maximum daily PRs: 5 unless the backlog is explicitly cleared.
- CI minutes and cloud quotas must remain within free-tier limits.

## Blockers

If a task cannot converge:

1. Add `agent:blocked`.
2. Open a follow-up issue with the failure evidence.
3. Move to the next unblocked task.
4. Do not bypass tests, checks, or protection rules.

## Production safety

- Deploy only from tags on `main`.
- Back up before migrations.
- Use additive migrations.
- Run smoke tests after deploy.
- Roll back automatically on failure.
- Open an incident issue after rollback.
- Respect the kill switch.
