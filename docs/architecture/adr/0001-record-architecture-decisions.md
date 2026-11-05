# ADR-0001: Record Architecture Decisions

## Status

Accepted.

## Context

JabaSurvivol is built autonomously and across Unity, .NET, infrastructure, and game design. Decisions need to survive context changes, agent handoffs, and future maintenance.

## Decision

Use lightweight ADRs under `docs/architecture/adr/`.

Each ADR includes:

- Title and number.
- Status.
- Context.
- Decision.
- Alternatives considered.
- Consequences.
- Migration or follow-up actions.

## Consequences

- Architectural decisions are discoverable and reviewable.
- Agents can rely on documented decisions instead of inferring intent.
- Small decisions may not need an ADR; irreversible or cross-cutting decisions do.
