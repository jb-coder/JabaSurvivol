# ADR-0002: Monorepo and Gitflow with Autonomous Review

## Status

Accepted.

## Context

The project includes a Unity client, .NET backend, infrastructure, agents, and design documentation. Changes must be reviewable, traceable, and safe to deliver autonomously.

## Decision

Use a monorepo with Gitflow:

- `main` for production.
- `develop` for integration.
- Short-lived `feature/*`, `bugfix/*`, `chore/*`, `docs/*`, and `test/*` branches.
- `release/*` and `hotfix/*` branches for releases.
- `backmerge/*` branches to synchronize `main` back to `develop`.

Every change enters through a pull request. The required reviewer is the independent `pr-reviewer` identity. The builder never approves its own PR. Auto-merge completes the workflow when all gates pass.

## Alternatives considered

- Trunk-based development: simpler, but the user explicitly requested Gitflow.
- Direct commits to `develop`: faster, but violates review and traceability.
- Human-only review: conflicts with the autonomy requirement.

## Consequences

- Branches must stay short-lived to avoid Gitflow overhead.
- Merge strategy differs by branch type: squash for work branches, merge commit for release and hotfix.
- Required checks and rulesets are mandatory.
- Two bot identities are needed to avoid self-approval.
