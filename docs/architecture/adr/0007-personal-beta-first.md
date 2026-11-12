# ADR-0007: Personal Beta-First Delivery and Deferred Monetization

## Status

Accepted. Partially superseded by ADR-0008 for VPS timing, Oracle scope,
v1.0.0 scope, and donation policy.

## Context

Version 0.5.0 implemented accounts, cloud saves, leaderboards, telemetry, remote
config, the BFF, container images, and deployment automation. The automated API
flow, packaging checks, and service tests pass. The remaining online verification
needs a completed Android phone and tablet run.

The intended first audience is five known people using a personal build. This is
not a public launch. The current constraints are:

- Oracle Always Free Ampere A1 capacity has not been available in the Madrid
  home region, and capacity is not guaranteed.
- A paid VPS is affordable but unnecessary while the game is still being
  polished.
- The project must not publish payment features before the core game is stable.
- The repository must not claim a hosted deployment that has not happened.

## Decision

### 1. Optimize for a five-person personal beta first

The immediate goal is a stable, polished single-player experience with optional
online backup, leaderboard, telemetry, and remote config. Scale, public launch,
and monetization are not current goals.

### 2. Use a local backend and private networking for the first beta

The first beta runs the backend on a development machine. Testers connect over
Tailscale. No public ingress, Oracle cluster, or paid VPS is required for this
phase.

The local beta must still exercise real accounts, saves, conflicts,
leaderboards, telemetry choices, and remote-config changes.

### 3. Treat a VPS as a later beta infrastructure step

A small VPS is introduced only when at least one concrete trigger exists:

- testers need availability without the development machine;
- the beta needs a stable public URL;
- the project needs realistic latency, backups, or deployment evidence;
- Oracle capacity remains unavailable and the local beta is complete.

The VPS is selected on measured need, not speculation. Contabo and Hetzner are
the initial candidates. The chosen architecture must account for x86 versus
ARM64 images, backups, TLS, least-privilege access, and monitoring.

### 4. Park Oracle-specific provisioning

Superseded by ADR-0008: Oracle is out of scope and #81 is closed. The eventual
hosting target is a VPS, provisioned only at the final handoff.

Issue #81 is removed from the v0.5.0 milestone and remains a future,
human-dependent infrastructure task. The project does not wait for Oracle
capacity and does not treat Oracle as a requirement for the personal beta.

### 5. Keep payments last

Premium currency, payments, the cosmetic store, and monetization remain out of
scope. The planned order is:

1. Polish and local beta.
2. Optional VPS beta.
3. Localization.
4. Free release readiness.
5. Payments and cosmetic Cores.

Payments remain server-authoritative and cosmetic-only. No loot boxes,
randomized paid rewards, or pay-to-win mechanics are introduced.

### 6. Do not fake release or deployment evidence

The project may publish a source release when it is ready, but it must state
that no hosted deployment occurred if that is true. Android evidence must come
from the stated device or emulator and must not be relabeled.

## Consequences

- The v0.5.0 milestone can close after #82 completes, without waiting for #81.
- Issue #27 remains CI debt and does not block polish.
- A new milestone, `v0.5.1 Polish and Local Beta`, tracks polish and beta work.
- Stale branches and local project snapshots are removed.
- The next infrastructure spend is explicit and tied to a beta trigger.
- The personal beta remains possible at zero infrastructure cost.

## Alternatives considered

### Wait for Oracle A1 capacity

Rejected as a blocker. Capacity is not guaranteed and the project can progress
without it.

### Deploy to a paid VPS immediately

Deferred. It adds cost and operational work before the game has been polished
with testers.

### Refactor to a serverless platform now

Deferred. It would require merging services, replacing k3s and PostgreSQL
assumptions, and changing the deployment model before the game is validated.

### Start monetization to fund hosting

Rejected. Payments add compliance, fraud, store, and server-authority work. The
product rules require monetization to come last.

## Non-goals

- Public launch, marketing, or app-store submission.
- Multi-node high availability, autoscaling, or a WAF.
- Localization.
- Premium currency or payments.
- Oracle provisioning.
