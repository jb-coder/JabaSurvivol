---
description: Plan, implement, test, review, and merge a feature autonomously.
agent: orchestrator
---

Deliver this feature autonomously: $ARGUMENTS

Read `AGENTS.md`, the linked issue, and the relevant documents under `docs/` first.

Workflow:

1. Confirm the smallest playable or verifiable increment.
2. Create a short-lived branch following Gitflow.
3. Delegate implementation to the appropriate specialist agent.
4. Require tests, English-only content, and no attribution policy compliance.
5. Build the solution and the Android APK when Android-visible behavior changes.
6. Open a pull request with the template and evidence.
7. Request review from `pr-reviewer`.
8. Address blocking feedback and re-request review.
9. Merge only after approval and all required checks pass.
10. Update the changelog and project board.

Do not bypass gates. If the task cannot converge after three review cycles, label it `agent:blocked`, open a follow-up issue, and report the blocker.
