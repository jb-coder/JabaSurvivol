# Agent Tools

PowerShell scripts used by agents and CI.

- `check-commit-style.ps1`: validates Conventional Commits.
- `check-forbidden-attribution.ps1`: rejects forbidden attribution phrases.
- `check-pr-policy.ps1`: validates PR branch flow, title, labels, and policy.
- `generate-build-info.ps1`: writes a Unity `BuildInfo` source file with version and commit.
- `gh-builder.ps1`: runs `gh` with `JABA_BUILDER_TOKEN` or the current `gh` login.
- `gh-reviewer.ps1`: runs `gh` with a reviewer token.
- `gh-app-token.ps1`: mints a short-lived installation token for the reviewer GitHub App.
- `GitHubApp.psm1`: shared JWT and installation token helpers.

## Usage

```powershell
pwsh tools/agent/gh-builder.ps1 pr create --repo jb-coder/JabaSurvivol --base develop --head feature/JAB-1

pwsh tools/agent/gh-reviewer.ps1 pr review 1 --repo jb-coder/JabaSurvivol --approve
pwsh tools/agent/gh-reviewer.ps1 api -X POST repos/jb-coder/JabaSurvivol/issues/1/labels -f "labels[]=agent:approved"
pwsh tools/agent/gh-reviewer.ps1 api -X DELETE repos/jb-coder/JabaSurvivol/issues/1/labels/agent%3Achanges-requested
```

The reviewer wrapper uses `JABA_REVIEWER_TOKEN` when set. Otherwise it reads the GitHub App config from `%USERPROFILE%\.config\jaba-survivol\github-app\config.json` and mints a short-lived token.

Never commit tokens. Never print tokens. All scripts are English-only.
