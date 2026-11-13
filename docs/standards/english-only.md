# English-Only Policy

## Scope

Everything committed to this repository must be in English, except approved
locale data tables and approved localization reference material:

- Source code, comments, XML documentation, test names.
- UI source strings, localization keys, asset names, folder names.
- Commit messages, PR titles and descriptions, issue titles and bodies.
- Documentation, ADRs, runbooks, release notes.
- Logs, error messages, metrics, and dashboard labels.

Approved locale tables under the Unity localization resources may contain the
translated product strings for their locale. Localization reference material
under `docs/localization/` may contain target-language terms and short examples;
its explanatory prose must remain English, and unreviewed target terms must be
marked as pending human review.

The user may speak Spanish in conversation. The repository remains English-only
outside approved locale tables and localization reference material.

## Localization

- English is the source locale and must remain complete.
- v0.6.0 activates English and Spanish with a device-language default and a
  runtime selector.
- User-facing strings must use localization keys from day one.
- Do not hardcode user-facing text in Unity scenes or scripts.
- Every non-English locale, including Spanish, requires human translation
  review before release.
- Additional locales require a reviewed ADR, a complete locale table, and human
  translation review.
- Product translations live only in locale tables; the glossary may list
  canonical target-language terms. Code, documentation, commit messages, and
  repository metadata stay English.

## Enforcement

- Reviewers reject mixed-language commits, PRs, issues, code, and documentation,
  except approved locale tables and localization reference material.
- The PR policy check rejects non-English repository metadata when detectable.
- Exceptions require a documented policy or ADR.
