# Pull Request Standards

## Branch flow

| Branch | From | PR to | Merge |
|---|---|---|---|
| `feature/*` | `develop` | `develop` | Squash |
| `bugfix/*` | `develop` | `develop` or `release/*` | Squash |
| `chore/*` | `develop` | `develop` | Squash |
| `docs/*` | `develop` | `develop` | Squash |
| `test/*` | `develop` | `develop` | Squash |
| `release/*` | `develop` | `main` | Merge commit |
| `hotfix/*` | `main` | `main` | Merge commit |
| `backmerge/*` | `main` | `develop` | Merge commit |
| `dependabot/*` | Dependabot | `develop` | Squash |

Every change enters through a pull request. No direct pushes to `main` or `develop`.

## PR requirements

- Conventional Commits title.
- Linked issue.
- Completed PR template.
- Small scope: one feature, bug, or chore.
- Verification evidence: commands, results, and artifacts.
- Android APK for Android-visible changes.
- Screenshots or video for visual changes.
- Migration note for save or database changes.
- No secrets, keystores, credentials, or build output.
- English-only repository content; approved locale tables excepted.
- No AI attribution.

## Review

- `pr-reviewer` is the required reviewer.
- The reviewer may approve, comment, or request changes.
- The builder addresses blocking feedback and re-requests review.
- After three failed cycles, the PR is labelled `agent:blocked` and a follow-up issue is opened.
- Auto-merge is enabled only after approval and all required checks pass.

## Required checks

- `pr-policy`
- `agent-guardrails`
- `security`
- `reviewer-approval`

The `reviewer-approval` check is created by the reviewer GitHub App on the current head commit. A new push invalidates it until the reviewer approves again. The `agent:approved` label is a visibility signal; `pr-policy` rejects `agent:changes-requested`.

Path-specific checks such as `backend-ci`, `unity-tests`, and `android-build` run when their paths change but are not required by the current ruleset until a summary gate exists.


## Merge

- Squash merge for feature, bugfix, chore, docs, and test branches.
- Merge commit for release, hotfix, and backmerge branches.
- Delete the head branch after merge.
- Tags are created only on `main`.
