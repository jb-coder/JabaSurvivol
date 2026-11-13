---
description: Review a pull request and approve, comment, or request changes.
agent: pr-reviewer
---

Review pull request $ARGUMENTS.

Use `pwsh tools/agent/gh-reviewer.ps1` for GitHub operations. Read `AGENTS.md` and `docs/agents/review-checklist.md`. Inspect the diff, linked issue, acceptance criteria, tests, build evidence, security, performance, English-only repository content (approved locale tables excepted), and no-attribution policy.

Post a review with a clear verdict:

- `APPROVE`: use `gh-reviewer.ps1 pr review --approve`, add `agent:approved` with `gh-reviewer.ps1 api`, remove `agent:changes-requested`, and create the `reviewer-approval` success check with `gh-reviewer-approval.ps1`.
- `COMMENT`: non-blocking suggestions only.
- `REQUEST_CHANGES`: add `agent:changes-requested` with `gh-reviewer.ps1 api`, remove `agent:approved`, create a `reviewer-approval` failure check, and list exact required changes.

Do not use `gh pr edit` for labels; use the REST label endpoints documented in the reviewer agent prompt.

Never edit code, commit, push, or merge. Do not mention AI, assistants, or generation tools. Do not claim manual human testing.
