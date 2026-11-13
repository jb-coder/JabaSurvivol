# Architecture

- `adr/`: architecture decision records.
- `adr/0005-shared-core-upm-package.md`: shared core source as a local UPM package.
- `adr/0006-backend-and-cloud-mvp.md`: v0.5.0 trust boundaries, public routes,
  event envelope, save revision contract, and remote-config shape.
- `adr/0007-personal-beta-first.md`: personal five-person beta, local-first
  infrastructure, deferred VPS, and payments last. Partially superseded by
  ADR-0008.
- `adr/0008-local-first-beta-with-donations.md`: local plus Tailscale until the
  final handoff, deferred VPS, Oracle out of scope, no payments or premium
  currency, and optional PayPal donations with no rewards.
- `adr/0009-localization-architecture.md`: English and Spanish locale tables
  behind the existing localization facade, device-language default, runtime
  selector, English fallback, and human-reviewed translations.
- `c4/`: reserved for C4 diagrams and context maps.

Current target:

```text
Unity Android/Windows client
            |
            v
      BFF / API gateway
       /             \
 Identity API      Game API
       \             /
        PostgreSQL
             ^
             |
     Telemetry worker
```

Telemetry events enter through the Game API. The telemetry worker aggregates
daily totals from PostgreSQL. PostgreSQL is the durable boundary; RabbitMQ is
deferred until a documented need appears.

Start with a modular backend. Extract services only when a documented need appears.
