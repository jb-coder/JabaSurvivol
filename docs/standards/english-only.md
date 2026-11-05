# English-Only Policy

## Scope

Everything committed to this repository must be in English:

- Source code, comments, XML documentation, test names.
- UI strings, localization keys, asset names, folder names.
- Commit messages, PR titles and descriptions, issue titles and bodies.
- Documentation, ADRs, runbooks, release notes.
- Logs, error messages, metrics, and dashboard labels.

The user may speak Spanish in conversation. The repository remains English-only.

## Localization

- English is the only active locale until the localization phase.
- User-facing strings must use localization keys from day one.
- Do not hardcode user-facing text in Unity scenes or scripts.
- The localization phase adds additional locales, fonts, store metadata, and language selection.

## Enforcement

- Reviewers reject mixed-language commits, PRs, issues, code, and documentation.
- The PR policy check rejects non-English repository metadata when detectable.
- Exceptions require an ADR and a documented reason.
