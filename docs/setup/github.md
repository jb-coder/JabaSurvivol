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

## Identities

- **Builder:** the repository owner account `jb-coder`. It creates branches and PRs, enables auto-merge, and merges. It uses the current `gh` authentication, or `JABA_BUILDER_TOKEN` when provided.
- **Reviewer:** the GitHub App `jaba-survivol-reviewer-jb`. It is not a user account. It approves or requests changes and manages `agent:approved` and `agent:changes-requested`.
- **Deployer:** optional short-lived OIDC credentials for Oracle and GHCR.

The builder must not approve its own PR. The reviewer app must not edit code.

The reviewer app is created and installed with:

```powershell
pwsh tools/bootstrap/New-GitHubReviewerApp.ps1
```

Its local credentials are stored outside the repository at:

```text
%USERPROFILE%\.config\jaba-survivol\github-app
```

See `docs/setup/github-app.md`.

### Local agent tokens

The builder wrapper falls back to the current `gh` login when `JABA_BUILDER_TOKEN` is not set. The reviewer wrapper mints a short-lived installation token from the GitHub App credentials.

```powershell
# Optional least-privilege builder token.
$env:JABA_BUILDER_TOKEN = "<builder token>"

# Optional override; otherwise the reviewer wrapper uses the GitHub App.
$env:JABA_REVIEWER_TOKEN = "<reviewer token>"
```

Use the wrappers for all GitHub writes:

```powershell
pwsh tools/agent/gh-builder.ps1 pr create --repo jb-coder/JabaSurvivol --base develop --head feature/JAB-1-unity-bootstrap
pwsh tools/agent/gh-reviewer.ps1 pr review 1 --repo jb-coder/JabaSurvivol --approve
pwsh tools/agent/gh-reviewer.ps1 api -X POST repos/jb-coder/JabaSurvivol/issues/1/labels -f "labels[]=agent:approved"
pwsh tools/agent/gh-reviewer.ps1 api -X DELETE repos/jb-coder/JabaSurvivol/issues/1/labels/agent%3Achanges-requested
```

The wrappers set `GH_TOKEN` and `GH_HOST` only for the duration of the command. They never print the token.

## Rulesets

### `main`

- Require a pull request.
- Require status checks: `pr-policy`, `agent-guardrails`, `security`.
- `pr-policy` enforces an approval from the reviewer app on the current head commit and the `agent:approved` label.
- Require conversation resolution.
- Block force pushes.
- Block deletions.
- Do not allow bypass.

### `develop`

- Require a pull request.
- Require status checks: `pr-policy`, `agent-guardrails`, `security`.
- `pr-policy` enforces an approval from the reviewer app on the current head commit and the `agent:approved` label.
- Require conversation resolution.
- Block force pushes.
- Block deletions.
- Do not allow bypass.

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
- `needs-human`, `needs-playtest`, `blocked`, `do-not-merge`

## Branch policy check

The `pr-policy` workflow validates:

- Source and target branch pairing.
- Conventional PR title.
- Linked issue.
- Forbidden attribution phrases.
- `agent:approved` label.
- `do-not-merge` absence.
- English-only metadata when detectable.

## Push the local repository

```powershell
git remote add origin https://github.com/jb-coder/JabaSurvivol.git
git push -u origin main
git push -u origin develop
```

## Bootstrap checklist

1. Create the repository.
2. Push the initial commit.
3. Create `develop`.
4. Create the reviewer GitHub App with `pwsh tools/bootstrap/New-GitHubReviewerApp.ps1`.
5. Configure rulesets and required checks.
6. Add secrets and variables.
7. Create labels with `pwsh tools/bootstrap/Initialize-GitHub.ps1 -Owner jb-coder -Repository JabaSurvivol`.
8. Enable Dependabot and secret scanning.
9. Set `UNITY_CI_ENABLED=true` after Unity is configured.
10. Set `DEPLOY_ENABLED=true` after Oracle is configured.
11. Set `AGENT_DRY_RUN=false` after the first successful review cycle.
