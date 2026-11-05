# Security Standards

## Secrets

- No secrets in Git, logs, PRs, screenshots, or build artifacts.
- Use GitHub Environments, short-lived tokens, and OIDC where possible.
- Unity license secrets are available only to CI.
- Keystores are stored outside the repository and injected at build time.
- Rotate credentials on a schedule and after any suspected exposure.

## Authentication

- Use short-lived access tokens and rotating refresh tokens.
- Store refresh tokens in platform-secure storage.
- Never trust client-provided user identity, currency, or leaderboard values.
- Premium wallet and purchase state are server-authoritative.
- Rate-limit authentication and sensitive endpoints.

## API

- Validate input at the boundary.
- Use ProblemDetails for errors without leaking internals.
- Apply authorization at the resource level, not only at the route level.
- Version public contracts under `/api/v1`.
- Expose only the BFF through ingress.

## Payments

- Implement payments last.
- Validate receipts server-side.
- Use idempotency keys for purchase grants.
- Handle refunds and chargebacks.
- Do not trust the client for currency balances.
- No loot boxes or randomized paid rewards.
- Require privacy policy, terms, age controls, tax, and store compliance before real-money launch.

## Supply chain

- Pin dependencies and use lock files.
- Run Dependabot, CodeQL, Trivy, and secret scanning.
- Review licenses and record assets in `THIRD_PARTY_NOTICES.md`.
- Generate SBOMs for release artifacts.
- Do not run untrusted scripts from pull requests.

## Containers and cloud

- Non-root containers.
- Read-only root filesystem where possible.
- Least-privilege RBAC and service accounts.
- Network policies.
- Firewall only required ports.
- No paid resources.
