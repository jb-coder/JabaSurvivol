# GitHub Setup

## Repository

- Name: `JabaSurvivol`.
- Visibility: public is recommended for free standard GitHub Actions runners.
- Default branch: `main`.
- Create `develop` from `main` after the initial commit.

## Gitflow branches

- `main`: production.
- `develop`: integration.
- `feature/*`, `bugfix/*`, `chore/*`, `docs/*`, `test/*`: short-lived work branches.
- `release/*`: release stabilization.
- `hotfix/*`: production fixes.
- `backmerge/*`: sync `main` back to `develop`.

## Bot identities

Create two GitHub Apps or dedicated bot accounts:

### `jaba-builder`

- Contents: read and write.
- Pull requests: read and write.
- Issues: read and write.
- Actions: read and write.
- Checks: read.

### `jaba-reviewer`

- Contents: read.
- Pull requests: read and write.
- Issues: read and write.
- Checks: read and write.

The builder creates branches and PRs, enables auto-merge, and merges. The reviewer approves or requests changes. The builder must not approve its own PR.

Optional: `jaba-deployer` with short-lived OIDC credentials for Oracle and GHCR.

## Rulesets

### `main`

- Require a pull request.
- Require approval from `jaba-reviewer`.
- Require status checks: `pr-policy`, `backend-ci`, `security`.
- Require conversation resolution.
- Block force pushes.
- Block deletions.
- Do not allow bypass.

### `develop`

- Require a pull request.
- Require approval from `jaba-reviewer`.
- Require status checks: `pr-policy`, `backend-ci`.
- Require conversation resolution.
- Block force pushes.
- Block deletions.

### Tags

- Protect `v*`.
- Allow only the release workflow or release manager to create tags.

## Merge settings

- Enable squash merge.
- Enable merge commit.
- Disable rebase merge.
- Automatically delete head branches.

## Environments

- `production`: deployment branch policy `main` only; secrets scoped; no human reviewers required.
- `unity-ci`: Unity license secrets.
- `oracle`: Oracle and k3s deployment secrets.

## Repository variables

- `UNITY_CI_ENABLED`
- `DEPLOY_ENABLED`
- `AGENT_PAUSED`
- `AGENT_DRY_RUN`
- `AGENT_MAX_PR_SIZE`
- `AGENT_MAX_REVIEW_CYCLES`
- `AGENT_MAX_DAILY_PRS`
- `AGENT_MAX_DAILY_DEPLOYS`
- `AGENT_MAX_CI_MINUTES`

## Secrets

- `UNITY_LICENSE` or `UNITY_SERIAL`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- `ORACLE_SSH_KEY`
- `ORACLE_HOST`
- `ORACLE_USER`
- `KUBECONFIG`
- `SOPS_AGE_KEY`
- `GRAFANA_TOKEN`
- `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` (later)

Never commit secrets.

## Labels

Create:

- `type:feature`, `type:bug`, `type:chore`, `type:docs`, `type:test`
- `area:game`, `area:api`, `area:ci`, `area:infra`, `area:docs`
- `priority:P0`, `priority:P1`, `priority:P2`, `priority:P3`
- `status:needs-review`, `status:in-review`, `status:changes-requested`, `status:approved`
- `agent:approved`, `agent:changes-requested`, `agent:blocked`, `agent:paused`
- `needs-playtest`, `blocked`, `do-not-merge`

## Branch policy check

The `pr-policy` workflow validates:

- Source and target branch pairing.
- Conventional PR title.
- Linked issue.
- Forbidden attribution phrases.
- `agent:approved` label.
- `do-not-merge` absence.
- English-only metadata when detectable.

## Bootstrap checklist

1. Create the repository.
2. Push the initial commit.
3. Create `develop`.
4. Create the builder and reviewer identities.
5. Configure rulesets and required checks.
6. Add secrets and variables.
7. Create labels.
8. Enable Dependabot and secret scanning.
9. Set `UNITY_CI_ENABLED=true` after Unity is configured.
10. Set `DEPLOY_ENABLED=true` after Oracle is configured.
