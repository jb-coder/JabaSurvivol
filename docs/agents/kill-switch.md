# Kill Switch

## Repository variable

Set the repository variable `AGENT_PAUSED` to `true` to stop autonomous merges and deployments.

The orchestrator and workflows must check this variable before:

- Enabling auto-merge.
- Merging a PR.
- Creating a release.
- Deploying to production.
- Running destructive migrations.

## Labels

- `do-not-merge`: blocks merge regardless of checks.
- `agent:paused`: pauses work on that PR or issue.
- `agent:blocked`: marks a task that cannot converge.
- `needs-human`: reserved for legal, billing, identity, or account-level actions.

## Dry run

Set the repository variable `AGENT_DRY_RUN` to `true` to simulate merges and deployments without executing them.

## Limits

Use repository variables to control:

- `AGENT_MAX_PR_SIZE`
- `AGENT_MAX_REVIEW_CYCLES`
- `AGENT_MAX_DAILY_PRS`
- `AGENT_MAX_DAILY_DEPLOYS`
- `AGENT_MAX_CI_MINUTES`

## Emergency

If an agent behaves unexpectedly:

1. Set `AGENT_PAUSED=true`.
2. Add `do-not-merge` to affected PRs.
3. Revoke bot tokens.
4. Inspect recent PRs and deployments.
5. Roll back if needed.
6. Document the incident.

## Note

The kill switch is a safety control, not a review gate. It exists so a human can stop automation without editing code. Normal day-to-day work does not require human intervention.
