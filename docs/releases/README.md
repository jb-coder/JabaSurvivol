# Releases

## Versioning

Use Semantic Versioning: `MAJOR.MINOR.PATCH`.

Tags are created only on `main`:

```text
v0.1.0
v0.1.1
v0.2.0
v0.3.0
v0.4.0
v0.5.1
v0.6.0
```

## Release checklist

- Release branch created from `develop`.
- Features frozen.
- Version and changelog updated.
- Supported locales and accepted layout gaps listed in the release notes.
- Full tests pass.
- Security checks pass.
- Android release APK or AAB builds.
- APK starts and passes smoke checks.
- Release PR approved and merged to `main`.
- Annotated tag created on `main`.
- GitHub Release published with artifacts and known issues.
- Backmerge PR created from `main` to `develop`.
- Deployment verified.
- Release notes contain no AI attribution.

## Per-locale release gate

For every supported non-English locale:

- [ ] `localization-review-<locale>.txt` is `Approved` and signed by a named human.
- [ ] The review covers the release commit or APK.
- [ ] Key parity, non-empty values, and placeholder parity pass.
- [ ] Missing-key fallback and pseudo-locale checks pass.
- [ ] Phone and tablet screenshots cover the main menu, HUD, pause, armory,
  and result screens.
- [ ] Screenshots show no truncation or overlap.
- [ ] Accented glyphs render with the active font.
- [ ] Known layout gaps are listed in the release notes.
- [ ] No unreviewed or stale translation is included.
