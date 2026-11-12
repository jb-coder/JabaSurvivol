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
```

## Release checklist

- Release branch created from `develop`.
- Features frozen.
- Version and changelog updated.
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
