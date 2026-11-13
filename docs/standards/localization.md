# Localization Standards

## Current phase

- English is the source locale and must remain complete.
- v0.6.0 introduces Spanish (`es`) with a device-language default and a runtime
  language selector, as decided in
  [`ADR-0009`](../architecture/adr/0009-localization-architecture.md).
- Additional locales, store metadata, RTL, and non-Latin font work remain out of
  scope until a reviewed ADR approves them.
- Do not hardcode user-facing strings.

## v0.6.0 scope

- English and Spanish locale tables with stable keys.
- Device-language detection with English fallback for unsupported languages.
- Manual override plus a system-default option, persisted locally.
- Runtime language changes without restarting the app or the current run.
- Key parity, missing-key fallback, empty-value checks, and pseudo-localization.
- Phone and tablet layout checks for both locales.
- Backend errors mapped to localized client messages by stable code.
- Human-reviewed translations; unreviewed machine translation is not allowed.

## Required seam

- Every user-facing string uses a localization key from day one.
- Keys follow the grammar in ADR-0009 and are grouped by feature. Existing keys
  are frozen; renames need a migration mapping.
- UI layouts support text expansion without breaking.
- Backend errors use stable error codes plus an English message.
- Displayed numbers, dates, and currency use the active locale's canonical
  culture. Persistence, API contracts, and logs stay invariant.
- Fonts include the glyphs needed for the active locales.

## Future localization phase

A later reviewed phase can add:

- Additional locales.
- Pluralization and gender rules.
- Right-to-left support if required.
- Store listing translations.
- Broader font and glyph coverage.

## Testing

- Fail builds with missing keys.
- Fail builds with empty localized values.
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
selector. Unresolved layout limitations must be recorded in the related issue
and release notes.
