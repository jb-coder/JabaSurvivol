## Summary

<!-- What changed and why? -->

## Related issue

Closes #

## Type of change

- [ ] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Test
- [ ] Documentation
- [ ] Localization
- [ ] CI or infrastructure

## Changes

<!-- Bullet list of the relevant changes. -->

## Localization

<!-- Required when locale tables, user-facing strings, or translated screens change. -->

- [ ] English source table updated first.
- [ ] Key and placeholder parity pass for every supported locale.
- [ ] Spanish review record is linked and signed by the named human reviewer.
- [ ] No unreviewed machine translation is included.
- [ ] Phone and tablet screenshots cover the changed screens in every locale.
- [ ] Pseudo-locale and layout checks pass.
- [ ] Known layout gaps are recorded.

## Verification

<!-- Commands, results, artifacts, screenshots, video, APK link. -->

```text
dotnet build JabaSurvivol.slnx -c Release
dotnet test JabaSurvivol.slnx -c Release
```

## Manual playtest

<!-- Optional. Do not claim human testing if it did not happen. -->

- [ ] Not performed
- [ ] Performed by a human

## Screenshots or video

<!-- Add links or images for visual changes. -->

## Risks

<!-- Known risks, rollback notes, migration notes. -->

## Checklist

- [ ] English-only repository content; approved locale tables and localization reference material excepted.
- [ ] No AI, assistant, LLM, tool, or generation attribution.
- [ ] No secrets, credentials, or keystores.
- [ ] Tests added or updated.
- [ ] Build passes.
- [ ] Android APK attached for Android-visible changes.
- [ ] Documentation and changelog updated when behavior changes.
- [ ] Human translation review signed for locale-table changes.
- [ ] Reviewer approval requested.
