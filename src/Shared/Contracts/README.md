# JabaSurvivol.Contracts

Public API and event contracts shared by the Unity client and the backend.

Rules:

- Version public HTTP contracts under `/api/v1`.
- Do not share database entities or internal domain models.
- Keep contracts small and backward-compatible.
- Use English names.
- Errors use ProblemDetails with a stable `code` extension.
- Never log tokens, save payloads, or telemetry payloads.

## v0.5.0 routes

| Method | Route | Auth |
|---|---|---|
| `POST` | `/api/v1/identity/register` | Anonymous |
| `POST` | `/api/v1/identity/login` | Anonymous |
| `POST` | `/api/v1/identity/refresh` | Anonymous |
| `POST` | `/api/v1/identity/logout` | Bearer |
| `GET` | `/api/v1/identity/me` | Bearer |
| `GET` | `/api/v1/game/config` | Anonymous |
| `GET` | `/api/v1/game/saves/primary` | Bearer |
| `PUT` | `/api/v1/game/saves/primary` | Bearer |
| `POST` | `/api/v1/game/leaderboards/highest-round/runs` | Bearer |
| `GET` | `/api/v1/game/leaderboards/highest-round` | Anonymous |
| `POST` | `/api/v1/telemetry/events` | Bearer |

The decisions behind these routes, including authentication, save revisions,
leaderboard tiebreaks, telemetry retention, and remote-config scope, are
recorded in
[ADR-0006](../architecture/adr/0006-backend-and-cloud-mvp.md).

Clients send camelCase JSON. The backend accepts camelCase and treats field
names case-insensitively at the boundary.
