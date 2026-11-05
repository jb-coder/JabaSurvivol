---
description: Reproduce, fix, test, review, and merge a defect autonomously.
agent: orchestrator
---

Fix this defect autonomously: $ARGUMENTS

Read `AGENTS.md`, the bug issue, and the relevant code first.

Workflow:

1. Reproduce the defect or document why reproduction is not possible.
2. Identify the smallest safe fix.
3. Add a regression test when feasible.
4. Implement the fix on a short-lived `bugfix/*` branch.
5. Run the relevant tests and build the Android APK when Android-visible behavior changes.
6. Open a pull request with reproduction steps, root cause, fix, and evidence.
7. Request review from `pr-reviewer`.
8. Merge only after approval and all required checks pass.
9. Update the changelog and close the issue.

Do not mask failures, weaken tests, or bypass gates.
