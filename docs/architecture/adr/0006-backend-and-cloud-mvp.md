# ADR-0006: Backend and Cloud MVP Trust Boundaries

## Status

Accepted.

## Context

The backend is a set of walking skeletons: Identity, Game, Telemetry Worker, and
the BFF expose only status routes. Version 0.5.0 introduces accounts, cloud
saves, leaderboards, telemetry, remote configuration, and the public HTTP
boundary. Without a shared decision record, parallel slices would invent
incompatible contracts, duplicate authority, and expose internal services.

The product rules constrain the design:

- Local progression stays authoritative for play and is never deleted by a
  network failure.
- No payments, premium currency, loot boxes, or pay-to-win mechanics exist in
  this version.
- Android is the primary platform and offline play must keep working.
- The infrastructure budget is one Oracle Always Free A1 VM with 2 OCPU and
  12 GB RAM.

## Decision

### 1. Identity

- Accounts use a case-insensitive unique username and a password. Usernames are
  3-32 characters from `A-Z`, `a-z`, `0-9`, `_`, and `-`. Passwords are 8-128
  characters and must contain at least one letter and one digit. The policy is
  enforced at the boundary and documented in API errors.
- Passwords are hashed with ASP.NET Core Identity's password hasher. The plain
  password is never stored, logged, or returned.
- A successful login returns a 15-minute JWT access token and a rotating
  30-day refresh token.
- Refresh tokens are stored only as SHA-256 hashes. Each login creates a token
  family. Refresh rotates the token: the current row is revoked and replaced.
  Reuse of a revoked token revokes the whole family and returns `401`.
- Logout revokes the active refresh family. It never touches game data.
- The JWT signing key, issuer, audience, and lifetimes come from environment
  configuration. There is no default production signing key and no key in Git.
- Authentication endpoints apply a strict per-IP rate limit and return
  ProblemDetails without revealing whether an account exists.
- Identity owns the `identity` PostgreSQL schema and its additive migrations.

### 2. Cloud saves

- There is exactly one cloud slot: `primary`.
- A save row stores the owning account, the slot, an integer revision starting
  at `0` for the first write, the schema version, the profile JSON document,
  and an `updatedAt` timestamp.
- `GET /api/v1/game/saves/primary` returns the document, revision, schema
  version, `updatedAt`, and an ETag.
- `PUT /api/v1/game/saves/primary` accepts `baseRevision` and `document`.
  The first write must use `baseRevision: 0`.
- A successful update increments the revision atomically. A stale
  `baseRevision` returns `409 Conflict` with the current revision and timestamp.
  The server never overwrites on conflict.
- Request bodies larger than 1 MiB return `413`. Malformed JSON, a missing
  `schemaVersion`, an unsupported schema version, or invalid profile fields
  return `422`.
- A save is readable and writable only by its owning account. Cross-account
  access returns `404` so the endpoint does not disclose slot existence.
- The local save remains the source of truth in v0.5.0. The cloud copy is a
  backup, not server-authoritative anti-cheat. The server validates shape and
  size and never recomputes progression.
- The save schema stays compatible with v0.1.0-v0.4.0: schema version `1` and
  the existing `Profile` field names. New fields are additive.
- Game owns the `game` PostgreSQL schema and the unique
  `(AccountId, Slot)` constraint.

### 3. Leaderboard

- The only board in v0.5.0 is `highest-round`.
- `POST /api/v1/game/leaderboards/highest-round/runs` accepts `runId`,
  `highestRound`, `totalKills`, `durationSeconds`, and `clientVersion`.
- `runId` is a UUID. The `(AccountId, RunId)` pair is unique, so retries are
  idempotent and return the existing accepted result.
- Bounds are validated: `highestRound` 1-10, `totalKills` 0-100,000,
  `durationSeconds` 1-86,400, and a non-empty `clientVersion` of at most 64
  characters.
- The board stores accepted runs and returns the best run per account.
- Ordering is `highestRound` descending, `totalKills` descending,
  `durationSeconds` ascending, then `achievedAt` ascending. The final tiebreak
  is deterministic by `runId`.
- `GET /api/v1/game/leaderboards/highest-round?limit=50` returns rank, username,
  highest round, total kills, duration, and achieved timestamp. The limit is
  capped at 100. No email, token, account identifier, or private data is
  returned.
- Submission is authenticated and per-account rate limited.
- v0.5.0 validates bounds only. Client-reported values are a documented
  limitation. Server-side run simulation is a later anti-cheat improvement and
  is a non-goal here.

### 4. Telemetry

- `POST /api/v1/telemetry/events` accepts an authenticated batch of 1-100
  events with a maximum body size of 64 KiB.
- Each event contains a UUID `eventId`, an allowlisted `eventType`, an ISO-8601
  UTC `occurredAt`, a UUID `sessionId`, and a typed payload of at most 4 KiB.
- Allowlisted event types: `run_start`, `run_end`, `round_clear`,
  `zombie_kill`, `boss_defeat`, `level_up`, `reward_selection`, and
  `weapon_switch`.
- Payload keys and value types are allowlisted per event type. Free-form
  strings are rejected outside documented enumerations. PII fields such as
  email, IP address, and device identifiers are rejected.
- Timestamps more than 24 hours in the past or 5 minutes in the future are
  rejected.
- Event IDs are unique per account. Duplicate events are counted in the
  response and ignored idempotently.
- Raw events are stored in PostgreSQL and retained for 30 days by a cleanup
  job in `Telemetry.Worker`.
- `Telemetry.Worker` produces idempotent daily aggregates for run count, kill
  count, boss defeats, level-ups, and average run duration. Aggregation is
  recomputed from raw events for the affected day, so retries and reruns do not
  double-count.
- PostgreSQL is the durable boundary. RabbitMQ is deferred. A broker is
  introduced only when a documented need appears: multi-service fan-out,
  cross-region delivery, or sustained ingestion that PostgreSQL cannot absorb
  within the free-tier budget.
- Telemetry is opt-in in the client and disabled by default.

### 5. Remote config

`GET /api/v1/game/config` returns a public, read-only, versioned document:

```json
{
  "version": 1,
  "minimumClientVersion": "0.5.0",
  "maintenanceMode": false,
  "cloudSavesEnabled": true,
  "leaderboardsEnabled": true,
  "telemetryEnabled": false,
  "telemetrySampleRate": 0.0
}
```

- Values come from the `RemoteConfig` configuration section with conservative
  defaults. An invalid document prevents the service from starting.
- `telemetrySampleRate` is constrained to `0.0`-`1.0`.
- The response includes an ETag and supports `304 Not Modified`.
- Cache headers are `public, max-age=60`.
- The document contains no economy values, premium state, secrets, or
  per-user data. Remote economy overrides are explicitly out of scope: they
  would bypass balance review, create pay-to-win risk, and break deterministic
  simulations. Economy changes ship with the client and a versioned release.

### 6. Public boundary

The BFF is the only public HTTP service. Identity, Game, and Telemetry
endpoints are reachable only inside the cluster. The BFF validates JWTs,
applies rate limits, propagates correlation IDs, maps upstream failures to
ProblemDetails, and forwards requests with YARP.

| Method | Public route | Auth | Upstream | Rate limit |
|---|---|---|---|---|
| `GET` | `/api/v1/status` | Anonymous | BFF | none |
| `POST` | `/api/v1/identity/register` | Anonymous | Identity | strict per IP |
| `POST` | `/api/v1/identity/login` | Anonymous | Identity | strict per IP |
| `POST` | `/api/v1/identity/refresh` | Anonymous | Identity | strict per IP |
| `POST` | `/api/v1/identity/logout` | Bearer | Identity | strict per IP |
| `GET` | `/api/v1/identity/me` | Bearer | Identity | per account |
| `GET` | `/api/v1/game/config` | Anonymous | Game | per IP |
| `GET` | `/api/v1/game/saves/primary` | Bearer | Game | per account |
| `PUT` | `/api/v1/game/saves/primary` | Bearer | Game | per account |
| `POST` | `/api/v1/game/leaderboards/highest-round/runs` | Bearer | Game | per account |
| `GET` | `/api/v1/game/leaderboards/highest-round` | Anonymous | Game | per IP |
| `POST` | `/api/v1/telemetry/events` | Bearer | Game | per account |

Internal services keep `/health` and `/alive` for probes. They are not exposed
through ingress.

### 7. Telemetry event envelope

```json
{
  "events": [
    {
      "eventId": "3f9d0a5e-1f7f-4d3e-9d0a-5e1f7f4d3e9d",
      "eventType": "run_end",
      "occurredAt": "2026-11-08T12:00:00Z",
      "sessionId": "a8f8b3ce-5f4e-4ad2-9c5e-3f0d7a9b1c2d",
      "payload": {
        "highestRound": 7,
        "totalKills": 212,
        "durationSeconds": 1430
      }
    }
  ]
}
```

Payload allowlist:

| Event type | Allowed keys |
|---|---|
| `run_start` | `startingRound` |
| `run_end` | `highestRound`, `totalKills`, `durationSeconds` |
| `round_clear` | `round`, `durationSeconds` |
| `zombie_kill` | `round`, `archetype` (`walker`, `runner`, `brute`) |
| `boss_defeat` | `round` |
| `level_up` | `level` |
| `reward_selection` | `round`, `rewardType` (`supplies`, `xp`, `ammo`, `armor`) |
| `weapon_switch` | `weapon` (`pistol`, `smg`, `shotgun`, `rifle`, `lmg`) |

### 8. Data ownership

- Each module owns its PostgreSQL schema: `identity`, `game`, and `telemetry`
  tables live in the `game` schema because the ingestion endpoint belongs to
  the Game module while the worker consumes the same tables.
- Contracts never expose database entities. Public DTOs live in
  `src/Shared/Contracts`.
- Migrations are additive and backward-compatible. Destructive migrations
  require a backup and a separate approved change.
- One service replica per workload in the MVP. Horizontal scaling requires an
  ADR.

## Alternatives considered

- **Server-authoritative progression in v0.5.0.** Rejected because it would
  require replay validation, deterministic server simulation, and offline
  reconciliation before the core loop is stable. It also risks deleting local
  progression on network failure.
- **Last-write-wins cloud saves.** Rejected because it silently destroys
  progress.
- **RabbitMQ from day one.** Rejected because the free-tier budget and the
  event volume do not justify a broker yet.
- **Remote economy configuration.** Rejected because it bypasses balance
  review, undermines deterministic tests, and creates pay-to-win risk.
- **One public service per module.** Rejected because it multiplies attack
  surface and ingress configuration without a present need.
- **Opaque refresh tokens without families.** Rejected because reuse detection
  would be impossible.

## Consequences

- Clients can play offline and treat online features as optional.
- Cloud saves are a backup, not an anti-cheat guarantee.
- Leaderboard values remain client-reported until server simulation exists.
- The backend stays a modular monolith with clear schema ownership.
- The BFF adds one network hop but centralizes authentication, rate limits, and
  error mapping.
- Integration tests require PostgreSQL through Testcontainers. EF InMemory is
  not used for persistence.
- All public errors use ProblemDetails with stable error codes.
- Tokens, save payloads, and telemetry payloads are never logged.

## Non-goals for v0.5.0

- Payments, premium currency, loot boxes, or pay-to-win mechanics.
- Localization beyond English.
- Email verification, password reset, social login, and account deletion.
- Multiple cloud save slots, server-side merge, and automatic conflict
  resolution.
- Seasons, friends, chat, push notifications, and multiple leaderboards.
- Server-side run simulation or replay validation.
- RabbitMQ, multi-node clusters, autoscaling, and paid cloud resources.
- PII, device identifiers, and third-party analytics SDKs.

## References

- `docs/standards/security.md`
- `docs/standards/pull-requests.md`
- `docs/game-design/economy.md`
- `src/Shared/Contracts/README.md`
- ADR-0003: Unity Client and .NET Backend
- ADR-0004: Persistent Progression
