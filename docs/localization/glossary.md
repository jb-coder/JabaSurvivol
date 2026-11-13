# Localization Glossary

## Status

These are the canonical English source terms and the Spanish terms currently
used by the locale table. The Spanish column is a working reference and remains
**pending human review**. A glossary change follows the same review path as a
locale-table change.

## How to use this glossary

- Use the English term as the source meaning, not a literal replacement.
- Keep the established UI style: short labels, uppercase where the current
  tables use uppercase, and no terminal punctuation in terse labels.
- Preserve placeholders, newlines, and the meaning of the surrounding screen.
- If a term needs a different translation for grammar or context, note the
  exception in the review record instead of silently diverging.
- Do not introduce a new term without updating this glossary and the locale
  tables together.

## Core terms

| English | Spanish (`es`) | Notes |
|---|---|---|
| Supplies | SUMINISTROS | Persistent currency. Plural in UI labels. |
| XP | XP | Do not translate the acronym. |
| Level | NIVEL | Character level. |
| Skill Point | PUNTO DE HABILIDAD | Singular label; plural is PUNTOS DE HABILIDAD. |
| Round | RONDA | One survival round. Not "asalto" or "vuelta". |
| Run | PARTIDA | A single run. "Restart" is REINICIAR, not a new term for run. |
| Armory | ARMERÍA | The between-run equipment screen. |
| Character | PERSONAJE | The character sheet and skill screen. |
| Skill | HABILIDAD | A character skill. |
| Turret | TORRETA | The survival active skill. |
| Shockwave | ONDA DE CHOQUE | Active skill name. |
| Dash | IMPULSO | Active skill name. |
| Boss | JEFE | Generic boss UI label. |
| The Warden | EL GUARDIÁN | Proper boss name; keep distinct from the generic JEFE. |
| Elite | ÉLITE | Elite round or enemy modifier. |
| Weapon | ARMA | Feminine noun. Weapon names use the established table values. |
| Armor | ARMADURA | Feminine noun. Equipped armor is "EQUIPADA"; equipped weapon is "EQUIPADO". |
| Light Armor | ARMADURA LIGERA | Armor tier. |
| Medium Armor | ARMADURA MEDIA | Armor tier. |
| Heavy Armor | ARMADURA PESADA | Armor tier. |
| Reload | RECARGAR | Action label. The in-progress status is RECARGANDO; the upgrade label is RECARGA. |
| Reserve Ammo | RESERVA | Short HUD label; use INF for infinite reserve where the table does. |
| Leaderboard | CLASIFICACIÓN | Highest-round leaderboard. |
| Cloud Save | GUARDADO EN LA NUBE | Account-backed save. |
| Account | CUENTA | Identity and cloud-save panel. |
| Highest Round | MEJOR RONDA | Leaderboard and progress label. |
| Next Run | SIGUIENTE PARTIDA | Victory screen action. |
| Restart | REINICIAR | Game-over action. |

## Do not translate

- The product name `JabaSurvivol`.
- The acronym `XP`.
- Internal keys, locale codes, file names, and API error codes.
- Player-authored names, account names, and leaderboard usernames.
- Numeric values, versions, commit labels, and build metadata.

## Style notes

- Spanish UI copy uses the established neutral imperative forms (INICIA, ELIGE,
  REINICIA) and infinitive action labels where the table uses them (RECARGAR).
  Do not switch to formal or regional forms without review.
- Keep the existing uppercase UI convention. Accented capitals must remain
  accented.
- Match grammatical gender to the noun: ARMADURA and ARMA are feminine;
  PERSONAJE and JEFE are masculine. Avoid adding articles to terse labels.
- Use RONDA for round and PARTIDA for run consistently. Do not translate "run"
  as "carrera" or "round" as "asalto".
- Keep the difference between the generic JEFE and the named EL GUARDIÁN.
- UI labels may omit articles. Full sentences must read naturally in Spanish.
- Preserve `{0}`-style placeholders exactly; reorder them only when the
  resulting sentence needs it.

## Change process

1. Update the English source term or its context first.
2. Update the Spanish term and every affected locale-table value.
3. Record the change and its review status in
   `docs/playtests/evidence/localization-review-es.txt`.
4. Run key, value, placeholder, pseudo-locale, and layout checks.
5. Obtain a named human review before release.

## References

- [`Localization standards`](../standards/localization.md)
- [`ADR-0009`](../architecture/adr/0009-localization-architecture.md)
- [`Spanish review record`](../playtests/evidence/localization-review-es.txt)
- [`English locale table`](../../unity/JabaSurvivol/Assets/Resources/Localization/en.json)
- [`Spanish locale table`](../../unity/JabaSurvivol/Assets/Resources/Localization/es.json)
