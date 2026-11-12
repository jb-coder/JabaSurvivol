# ADR-0003: Unity Client and .NET Backend

## Status

Accepted. Partially superseded by ADR-0007 and ADR-0008 for hosting. The beta
runs locally over Tailscale; the eventual deployment target is a VPS at final
handoff, and Oracle is out of scope.

## Context

The game is Android-first, 2D, touch-based, and intended to strengthen Unity employability. The project also needs a .NET backend for identity, cloud saves, leaderboards, telemetry, and future monetization.

## Decision

- Use Unity 6.3 LTS for the game client.
- Use .NET 10 LTS for the backend.
- Keep deterministic game rules in pure C# under `src/Game.Core`.
- Share those rules with Unity through a local UPM package.
- Start the backend as a modular monolith and extract services only when justified.
- Use Aspire 13.x for local orchestration.
- Deploy the backend at the final handoff to a VPS. Oracle is out of scope
  (ADR-0007, ADR-0008).

## Alternatives considered

- MAUI client: weaker for game feel and rendering.
- Godot: strong 2D engine, but Unity is the explicit employability choice.
- Unity-only backend: poor fit for secure accounts, leaderboards, and telemetry.
- Full microservices from day one: unnecessary complexity for the MVP.

## Consequences

- The client and backend can evolve independently.
- Game rules must remain free of `UnityEngine`.
- Unity and .NET must stay compatible with .NET Standard 2.1 for the shared core.
- The backend can grow without rewriting the game client.
