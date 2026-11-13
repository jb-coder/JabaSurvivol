# ADR-0009: Localization Architecture and Initial Locales

## Status

Accepted.

## Context

Version 0.6.0 introduces the first non-English product locale. The game must:

- Use the device language on first launch when it is supported.
- Let the player override the language and return to the device default.
- Change the language at runtime without restarting the app or the current run.
- Fall back to English when the device language or a key is unavailable.
- Keep English as the source locale for code, tests, documentation, and support.
- Support human-reviewed Spanish translations without allowing unreviewed machine
  translation into the product.

The Unity client already renders its UI programmatically and calls a static
`Localization` facade for every user-facing string. `Game.Core` is engine-free
and does not contain user-facing text. The project does not currently use the
Unity Localization Package or Addressables.

The policy change is bounded: English remains the repository language, and only
approved locale data tables may contain translated product strings.

## Decision

### 1. Keep the existing localization facade

- The Unity client keeps `Localization.Get` and the existing `Format` overloads
  as the only UI-facing localization API.
- The facade gains locale loading, current-locale state, a locale-changed event,
  and a system-default option.
- UI code must not read locale files or parse JSON directly.

### 2. Store locale tables as JSON resources

- Locale files live under `Assets/Resources/Localization/`.
- Initial files are `en.json` and `es.json`.
- Each file uses a serializable array shape so Unity's built-in `JsonUtility`
  can parse it without a new package:

```json
{
  "locale": "en",
  "displayName": "English",
  "entries": [
    { "key": "app.title", "value": "JABASURVIVOL" }
  ]
}
```

- Keys are stable, ASCII, dot-separated, and grouped by feature. Each segment
  starts with a lowercase letter and may use camelCase to preserve existing
  names. The grammar is
  `^[a-z][A-Za-z0-9]*(\.[a-z][A-Za-z0-9]*)+$`. Existing keys such as
  `cloud.useCloud`, `cloud.keepLocal`, and `cloud.msg.restoreFailed` are frozen;
  renames require a migration mapping.
- Duplicate keys, invalid keys, empty values, and missing English entries fail
  validation. Every key referenced by UI code must exist in the English table.
- English is the source table. Spanish must contain the same key set.
- Missing keys fall back to English and log a warning. If a key is missing from
  both tables, return the neutral `"..."` placeholder and log an error. Raw keys
  are never shown to the player.
- The tables are small enough to load through `Resources.Load<TextAsset>` at
  startup. Addressables are not required for v0.6.0.

### 3. Device default, override, and fallback

- On first launch, the active locale is derived primarily from
  `Application.systemLanguage`: `English` maps to `en`, `Spanish` maps to `es`,
  and any other value falls back to the two-letter
  `CultureInfo.CurrentUICulture` code. If neither maps to a supported locale,
  use `en`.
- Supported initial locales are `en` and `es`.
- Unsupported device languages fall back to `en`.
- The manual override is stored locally under the `localization.locale`
  preference. The values are `system`, `en`, and `es`.
- `system` is the default and clears the manual override.
- The preference is a client setting, not a cloud-save field. Changing it must
  not mutate the persistent progression profile or start a cloud revision.

### 4. Runtime changes

- The selector appears in the main menu and pause menu. The system option is
  labeled `System default` in English and `Idioma del sistema` in Spanish.
- `Localization.SetLocale` updates the active table and raises a locale-changed
  event.
- `GameSession` forwards the event to `GameUi`.
- Static labels, HUD text, pause menu, armory, character sheet, result screens,
  and dialogs refresh immediately.
- Dynamic HUD values continue to format on their existing update paths.
- The current run, profile, cloud state, and telemetry queue are preserved.

### 5. Display culture and persistence format

- UI display values use the canonical culture for the active locale: `en-US`
  for `en` and `es-ES` for `es`.
- Persistence, JSON, API contracts, ETags, timestamps, logs, and diagnostics use
  invariant culture.
- Device culture variants such as `es-MX` map to the supported locale while the
  display uses the canonical culture.
- Tests cover decimal separators, dates, and other displayed values for both
  locales.

### 6. Translation and review

- English is the source of truth.
- Spanish is translated and reviewed by a human before release.
- Unreviewed machine translation is not allowed in the product.
- A glossary covers core terms such as Supplies, Round, Run, Armory, Turret, and
  Boss.
- Adding a locale requires a reviewed ADR, a complete key set, key-parity tests,
  pseudo-locale checks, and phone/tablet layout evidence.
- Spanish review is recorded in the locale pull request and in
  `docs/playtests/evidence/localization-review-es.txt`, including the reviewer,
  date, key coverage, and unresolved items.

### 7. Backend errors

- Backend logs, diagnostics, and internal messages remain English.
- The client maps stable API error codes to localized messages.
- Unknown error codes use a generic localized message and never display a raw
  server payload.

### 8. Scope

- Initial locales: English and Spanish only.
- Additional locales, right-to-left layouts, non-Latin font coverage, and store
  metadata are out of scope until a later reviewed ADR.
- Complex pluralization and gender rules are out of scope; v0.6.0 strings are
  written to avoid them.
- Android per-app language settings and font fallback are optional follow-up
  work, not part of the core v0.6.0 decision.

## Consequences

- The `Localization` facade keeps its API and gains a JSON-backed locale store.
- `en.json` and `es.json` become the only non-English repository content.
- The UI needs a language selector and a refresh path.
- Tests cover device-language mapping, fallback, persistence, key parity,
  missing keys, pseudo-localization, display culture, and phone/tablet layouts.
- Spanish glyph checks cover `á é í ó ú ü ñ ¿ ¡` with the active
  `LegacyRuntime.ttf`/Arial font. If a glyph is missing, add a reviewed font
  fallback before release.
- No new Unity package or Addressables dependency is added in v0.6.0.
- The existing policy documents remain valid with the approved locale-table
  exception.
- The custom system does not provide pluralization, gender, RTL, or translator
  tooling. Those features require a later ADR or migration.

## Alternatives considered

### Unity Localization Package

Rejected for v0.6.0. It is the standard long-term option, but it adds asset
tables, Addressables workflow, package and build complexity, and a larger UI
migration for two locales. Revisit when the project needs more than two locales,
translator tooling, plural/gender rules, or asset localization.

### Keep hardcoded English strings

Rejected. It blocks device-language defaults, the runtime selector, key coverage,
and translation review.

### CSV or PO files

Rejected for v0.6.0. Both are translator-friendly, but they require a new parser
or workflow. The JSON array shape works with the existing `JsonUtility` path and
is sufficient for two locales. A migration to PO or Unity Localization remains
possible later.

### Backend-driven localization

Rejected. It adds latency, coupling, and offline failures to the main menu and
HUD. Locale data belongs in the client build.

### Device language only, without an override

Rejected. Players need to change the language independently of the device, and
the selector must support a system-default option.

### Store the locale in the cloud profile

Rejected for v0.6.0. It would create cloud-save revisions and cross-device
conflicts for a device preference. The locale stays local.

## Non-goals

- Additional locales beyond English and Spanish.
- Right-to-left support.
- Non-Latin font coverage.
- Pluralization and gender rules beyond the fixed en/es strings.
- Store listing translations.
- Translating documentation, logs, commits, issues, or repository metadata.
- Asset localization beyond UI strings.

## References

- `docs/standards/localization.md`
- `docs/standards/english-only.md`
- `docs/playtests/v0.5.1-beta-plan.md`
- `docs/architecture/adr/0008-local-first-beta-with-donations.md`
