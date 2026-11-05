---
description: Prepare and publish a versioned release through the Gitflow release path.
agent: devops
---

Prepare release $ARGUMENTS.

Read `AGENTS.md`, `docs/standards/pull-requests.md`, and `docs/releases/`.

Workflow:

1. Confirm the release branch was created from `develop`.
2. Freeze features; allow only bug fixes, documentation, version, and changelog changes.
3. Run the full test suite, security checks, and Android release build.
4. Verify the APK starts and passes smoke checks.
5. Open the release pull request to `main` with changelog and known issues.
6. Request review from `pr-reviewer`.
7. After merge and all checks pass, create an annotated tag `vX.Y.Z` on `main`.
8. Publish the GitHub Release with the APK and release notes.
9. Create a `backmerge/vX.Y.Z` pull request from `main` to `develop`.
10. Report the release tag, artifacts, and deployment status.

Do not create a release from an unmerged or failing branch. Do not mention AI, assistants, or generation tools.
