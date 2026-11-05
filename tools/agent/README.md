# Agent Tools

PowerShell scripts used by agents and CI.

- `check-commit-style.ps1`: validates Conventional Commits.
- `check-forbidden-attribution.ps1`: rejects forbidden attribution phrases.
- `check-pr-policy.ps1`: validates PR branch flow, title, labels, and policy.
- `generate-build-info.ps1`: writes a Unity `BuildInfo` source file with version and commit.
- `gh-builder.ps1`: runs `gh` with `JABA_BUILDER_TOKEN`.
- `gh-reviewer.ps1`: runs `gh` with `JABA_REVIEWER_TOKEN`.

## Usage

```powershell
$env:JABA_BUILDER_TOKEN = "..."
pwsh tools/agent/gh-builder.ps1 pr create --repo jb-coder/JabaSurvivol --base develop --head feature/JAB-1

$env:JABA_REVIEWER_TOKEN = "..."
pwsh tools/agent/gh-reviewer.ps1 pr review 1 --repo jb-coder/JabaSurvivol --approve
```

Never commit tokens. Never print tokens. All scripts are English-only.
