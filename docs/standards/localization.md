# Localization Standards

## Current phase

- English is the only active locale.
- Do not add a language selector, translated assets, or store metadata yet.
- Do not hardcode user-facing strings.

## Required seam

- Every user-facing string uses a localization key from day one.
- Keys are stable, English, and grouped by feature.
- UI layouts support text expansion without breaking.
- Backend errors use stable error codes plus an English message.
- Dates, numbers, and currency use culture-aware formatting.
- Fonts include the glyphs needed for future locales.

## Final localization phase

The localization phase happens immediately before monetization.

It includes:

- Additional locales.
- Language selection.
- Pluralization and gender rules.
- Font and glyph coverage.
- Right-to-left support if required.
- Store listing translations.
- Pseudo-localization and layout tests.
- Missing-key and empty-string checks.

## Testing

- Fail builds with missing keys.
- Run pseudo-localization layout tests.
- Verify phone and tablet layouts with expanded text.
- Verify screenshots for each supported locale.
