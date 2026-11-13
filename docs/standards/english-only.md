# English-Only Policy

## Scope

Everything committed to this repository must be in English, except approved
locale data tables:

- Source code, comments, XML documentation, test names.
- UI source strings, localization keys, asset names, folder names.
- Commit messages, PR titles and descriptions, issue titles and bodies.
- Documentation, ADRs, runbooks, release notes.
- Logs, error messages, metrics, and dashboard labels.

Approved locale tables under the Unity localization resources may contain the
translated product strings for their locale.

The user may speak Spanish in conversation. The repository remains English-only
outside the approved locale tables.

## Localization

- English is the source locale and must remain complete.
- v0.6.0 activates English and Spanish with a device-language default and a
  runtime selector.
- User-facing strings must use localization keys from day one.
- Do not hardcode user-facing text in Unity scenes or scripts.
- Additional locales require a reviewed ADR and human translation review.
- Translations live only in locale tables; code, documentation, commit
  messages, and repository metadata stay English.

## Enforcement

- Reviewers reject mixed-language commits, PRs, issues, code, and documentation.
- The PR policy check rejects non-English repository metadata when detectable.
- Exceptions require an ADR and a documented reason.
