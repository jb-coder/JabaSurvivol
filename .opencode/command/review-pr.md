---
description: Review a pull request and approve, comment, or request changes.
agent: pr-reviewer
---

Review pull request $ARGUMENTS.

Read `AGENTS.md` and `docs/agents/review-checklist.md`. Inspect the diff, linked issue, acceptance criteria, tests, build evidence, security, performance, English-only policy, and no-attribution policy.

Post a review with a clear verdict:

- `APPROVE`: add `agent:approved` and remove `agent:changes-requested`.
- `COMMENT`: non-blocking suggestions only.
- `REQUEST_CHANGES`: add `agent:changes-requested`, remove `agent:approved`, and list exact required changes.

Never edit code, commit, push, or merge. Do not mention AI, assistants, or generation tools. Do not claim manual human testing.
