# ADR-0008: Local-First Beta, Deferred VPS, and Donation-Only Support

## Status

Accepted.

## Context

Version 0.5.1 is the five-person local beta. It runs on the development PC and
is reachable over Tailscale. There is no VPS and no public ingress.

The project will not contract or deploy a VPS until the final handoff. Oracle
Always Free is not the target hosting path, and issue #81 is out of scope.

Payments, premium currency, the cosmetic store, and monetization are not being
implemented now. The owner wants an optional PayPal donation link for people who
want to support the initial phase.

## Decision

### 1. Local plus Tailscale until the final handoff

- v0.5.1 closes with local services, Tailscale, and the development PC.
- No v0.5.1 acceptance criterion depends on a VPS.
- The beta plan and the security checklist remain local and Tailscale-first.

### 2. VPS is a final handoff step

- The eventual hosting target is a VPS.
- No VPS is contracted, provisioned, or deployed during v0.5.1. The handoff
  starts after the local beta exit criteria are met and before any wider beta or
  release.
- Provider, region, size, architecture, orchestration, domain, TLS, backups,
  observability, and budget are decided at handoff.
- No paid resource is created before that decision.
- When the handoff starts, write a deployment ADR and create the deployment
  issues.

### 3. Oracle is out of scope

- Issue #81 is closed as out of scope.
- Oracle is not a requirement, fallback, or acceptance criterion.

### 4. No payments or premium currency now

- No in-game purchases, Cores, cosmetic store, battle pass, loot boxes, or
  pay-to-win mechanics.
- No wallet, receipt validation, or payment endpoints are implemented.
- v1.0.0 is rescoped to release readiness and optional donations.

### 5. Optional PayPal donations

- A PayPal donation link may be shown in the README, release notes, and project
  page.
- Donations are voluntary and outside the game economy.
- Donations grant no Cores, currency, items, cosmetics, content, priority, or
  gameplay advantage.
- The link must be clearly labeled as a donation, not a purchase.
- The link must follow PayPal and platform rules.
- The owner provides the PayPal URL.
- If an in-game entry is added later, it must open the external link and must
  not implement an in-game purchase flow.

## Consequences

- AGENTS.md, security standards, game design documents, and the repository
  milestone table are updated to match this ADR. The GitHub milestone is
  updated after merge.
- #81 closes as out of scope.
- No VPS issue or paid resource is created now.
- The donation link is a small later task, not a payment system.
- v0.5.1 can close without VPS services.

## Alternatives considered

### Use Oracle Always Free

Rejected. Capacity is not guaranteed, provisioning needs a human, and the owner
has chosen a VPS for the eventual handoff.

### Provision a VPS now

Rejected. It adds cost and operational work before the local beta is validated.
The VPS is a final handoff step.

### Implement payments now

Rejected. Payments add compliance, fraud, store, and server-authority work. They
are deferred until a later ADR explicitly reintroduces them.

### No donations at all

Allowed, but the owner wants a simple optional link for the initial phase.

## Non-goals

- Public launch, marketing, or app-store submission.
- VPS provisioning before the final handoff.
- Payments, premium currency, cosmetic store, or monetization.
- Localization.
