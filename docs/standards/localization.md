# Localization Standards

## Current phase

- English is the source locale and must remain complete.
- v0.6.0 introduces Spanish (`es`) with a device-language default and a runtime
  language selector, as decided in
  [`ADR-0009`](../architecture/adr/0009-localization-architecture.md).
- Additional locales, store metadata, RTL, and non-Latin font work remain out of
  scope until a reviewed ADR approves them.
- Do not hardcode user-facing strings.
- Spanish is a complete first translation but is **pending human review**. It
  must not be described as reviewed until
  `docs/playtests/evidence/localization-review-es.txt` is signed.
- The Spanish table was merged in #147 as a historical exception before this
  workflow existed. It remains on `develop`, must not be released, and blocks
  the v0.6.0 release until the review record is signed.

## Source of truth

- `unity/JabaSurvivol/Assets/Resources/Localization/en.json` is the source of
  truth for every product string.
- English source changes happen before or with the corresponding locale change.
  A changed English value marks its translations for re-review.
- Keys follow the grammar in ADR-0009 and are grouped by feature. Existing keys
  are frozen; renames need a migration mapping.
- Every key must exist in every supported locale.
- Format placeholders such as `{0}` must match across locales. Translators may
  reorder placeholders for grammar, but not drop, add, or change their indexes.
- Product translations live only in locale tables. The glossary may list
  canonical target-language terms; explanatory prose remains English.

## Roles and ownership

| Role | Responsibility |
|---|---|
| Source owner | Maintains English strings, keys, placeholders, and source changes. |
| Translator | Produces the locale-table translation against the current English source. |
| Spanish review owner | Accountable owner for the Spanish review: repository owner `jb-coder`. |
| Human linguistic reviewer | Named human who checks meaning, tone, glossary, grammar, and UI fit. Cannot be an agent. |
| Release approver | Confirms the signed review record before a locale ships. |
| `pr-reviewer` agent | Reviews code, tests, policy, and evidence. Its approval is not linguistic approval. |

The named human reviewer is recorded in the review record only after the review
happens. Do not invent a reviewer or approval.

## Translation workflow

1. **Freeze the source.** Finish English strings and keys for the slice before
   translating. Include placeholder changes in the same source change.
2. **Prepare the brief.** Link the issue, the affected screens, the glossary
   version, and any context the translator needs.
3. **Translate.** Update only the locale table values for the target locale.
   Keep keys, placeholders, newlines, and uppercase style unless the glossary
   or reviewer approves a change.
4. **Run checks.** Key parity, non-empty values, placeholder parity, fallback,
   pseudo-locale, and phone/tablet layout evidence must pass.
5. **Open the translation pull request.** Include the pending review record,
   screenshots, and check results. Apply `needs-human`; the PR must not merge
   while the record is pending.
6. **Human review.** The named human reviewer checks every changed string
   against the glossary and the UI. The reviewer records findings and either
   signs or rejects the review record.
7. **Merge.** After the record is signed, remove `needs-human` and merge through
   the normal review and check gate. New or changed unreviewed strings do not
   enter `develop`. The Spanish table merged in #147 is the only historical
   exception and remains release-blocked.
8. **Release.** The release approver checks the signed record and per-locale
   release checklist.

## Merge and release gate

The review gate has one rule: a non-English locale may enter `main` and ship
only with a signed review record.

- New or changed non-English strings: open the PR with a pending record and
  `needs-human`. The PR cannot merge until the named human signs the record and
  the label is removed.
- Existing Spanish table from #147: already on `develop` as a historical
  exception. It cannot ship until `localization-review-es.txt` is signed.
- The release checklist repeats the gate so a skipped test job cannot bypass it.

## Review and approval

The review record is
`docs/playtests/evidence/localization-review-<locale>.txt`. It records:

- Status: Pending or Approved.
- Named human reviewer.
- Date and reviewed commit or APK.
- Locale file and English source table.
- Key coverage and placeholder coverage.
- Glossary version.
- Translation method: `Human` or `Machine-assisted, fully human-reviewed`.
- Screens reviewed.
- Unresolved items.
- Explicit approval.

A review must cover:

- Meaning, tone, and game context.
- Glossary compliance.
- Grammar, gender, accents, and punctuation.
- Placeholder and newline preservation.
- Missing, stale, or untranslated values.
- Phone and tablet screenshots for changed screens.
- Truncation, overlap, and glyph coverage.

Re-review is required when:

- An English source value or key changes.
- A placeholder changes.
- A glossary term changes.
- A changed screen has no current screenshot.
- A reviewer finds a meaning or layout defect.

The `pr-reviewer` approval and the `reviewer-approval` check do not satisfy the
human translation review gate.

## Machine-assisted translation policy

Unreviewed machine translation is not allowed in the product. This includes
directly committing machine output, importing a generated locale table, or
shipping strings that no human reviewer understood and verified.

A machine-assisted draft may be used only as working material. The named human
reviewer must verify every changed value, keep or edit it deliberately, and
sign the review record. Do not record tool names in the repository; record only
`Machine-assisted, fully human-reviewed` in the review record when applicable.

## Glossary

The canonical term list is
[`docs/localization/glossary.md`](../localization/glossary.md). It covers
Supplies, Round, Run, Armory, Turret, Boss, and the other recurring game terms.
Use it for every new or changed string. Glossary changes follow the same source,
translation, and human-review path as locale-table changes.

## Adding a locale

Adding a locale requires a reviewed ADR before implementation. The checklist is:

- [ ] Reviewed ADR names the locale, fallback, and scope.
- [ ] Human translator and named human reviewer are available.
- [ ] Locale table is added under `Assets/Resources/Localization/`.
- [ ] Locale is added to `Localization.SupportedLocaleCodes`.
- [ ] `DisplayCulture` has a canonical display culture.
- [ ] `MapSystemLanguage` maps the device language and culture variants.
- [ ] Language selector and persistence accept the new locale.
- [ ] English fallback and missing-key behavior are covered.
- [ ] Key parity and non-empty tests cover the new locale.
- [ ] Placeholder parity tests cover the new locale.
- [ ] Pseudo-locale layout checks pass.
- [ ] Font and glyph coverage is verified for the locale.
- [ ] Phone and tablet screenshots cover the required screens.
- [ ] `localization-review-<locale>.txt` is signed by a named human.
- [ ] Release notes list supported locales and accepted layout gaps.
- [ ] No unreviewed machine translation is present.

## Release checklist per locale

For every supported locale, before a release:

- [ ] `localization-review-<locale>.txt` status is Approved and signed.
- [ ] The review record covers the release commit or APK.
- [ ] Key parity, non-empty values, and placeholder parity pass.
- [ ] Missing-key fallback and pseudo-locale checks pass.
- [ ] Phone and tablet screenshots are refreshed for the main menu, HUD,
  pause, armory, and result screens.
- [ ] Screenshots show no truncation or overlap for the locale.
- [ ] Accented glyphs render with the active font.
- [ ] Known layout gaps are listed in the release notes.
- [ ] No unreviewed or stale translation is included.

## Future localization phase

A later reviewed phase can add:

- Additional locales.
- Pluralization and gender rules.
- Right-to-left support if required.
- Store listing translations.
- Broader font and glyph coverage.

## Testing and evidence

- Fail builds with missing keys.
- Fail builds with empty localized values.
- Fail builds with placeholder mismatches.
- Run pseudo-localization layout tests with expanded text.
- Verify phone and tablet layouts with expanded text.
- Verify screenshots for each supported locale.

### Current automated checks

- Key parity and non-empty values for English and Spanish.
- English fallback for a missing active-locale key, with the raw key never shown.
- A test-only pseudo-locale derived from English with at least 30 percent
  expansion, accented characters, and preserved format placeholders.
- A PlayMode layout audit for the main menu, HUD, pause, game-over, and
  round-end screens under the pseudo-locale.
- A committed phone and tablet screenshot matrix for the main menu, HUD, pause,
  armory, and result screens in English and Spanish.

The pseudo-locale is never persisted and is not exposed in the language
selector. Character-sheet layout remains a documented gap outside the #141
pseudo-locale audit and screenshot matrix.

## References

- [`ADR-0009`](../architecture/adr/0009-localization-architecture.md)
- [`English-only policy`](english-only.md)
- [`Pull request standards`](pull-requests.md)
- [`Localization glossary`](../localization/glossary.md)
- [`Spanish review record`](../playtests/evidence/localization-review-es.txt)
