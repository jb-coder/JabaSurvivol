# Agent Tools

PowerShell scripts used by agents and CI.

- `check-commit-style.ps1`: validates Conventional Commits.
- `check-forbidden-attribution.ps1`: rejects forbidden attribution phrases.
- `check-pr-policy.ps1`: validates PR branch flow, title, labels, and policy.
- `generate-build-info.ps1`: writes a Unity `BuildInfo` source file with version and commit.

All scripts are English-only and must not print secrets.
