# GitHub Project

## Project

- Name: `JabaSurvivol Roadmap`
- Owner: `jb-coder`
- Number: `1`
- URL: https://github.com/users/jb-coder/projects/1
- Linked repository: `jb-coder/JabaSurvivol`

## Fields

| Field | Type | Values |
|---|---|---|
| Status | Single select | Backlog, Ready, In Progress, In Review, Changes Requested, In QA, Done, Blocked |
| Priority | Single select | P0, P1, P2, P3 |
| Area | Single select | Game, API, CI, Infra, Docs, Art |
| Size | Single select | S, M, L |
| Phase | Single select | Walking Skeleton, Rounds, Armory, Skills, Content, Backend, Localization, Art, Monetization, Release |
| Sprint | Iteration | Thirteen two-week iterations starting 2026-11-09 |
| Target | Date | Milestone due date |
| Milestone | Built-in | Repository milestone |

## Views

| View | Layout | Filter |
|---|---|---|
| All Work | Table | `is:open` |
| Backlog | Table | `is:issue is:open` |
| Delivery Board | Board | `is:open` |
| Roadmap | Roadmap | `is:issue` |
| Pull Requests | Table | `is:pr is:open` |
| My Work | Table | `assignee:@me is:open` |

## Milestones

| Milestone | Target |
|---|---|
| v0.1.0 Walking Skeleton | 2026-11-23 |
| v0.2.0 Rounds and Economy | 2026-12-07 |
| v0.3.0 Armory and Skills | 2026-12-21 |
| v0.4.0 Content and Boss | 2027-01-04 |
| v0.5.0 Backend and Cloud | 2027-01-18 |
| v0.5.1 Polish and Local Beta | 2026-12-14 |
| v0.6.0 Localization | 2027-02-01 |
| v0.7.0 Art and Visual Polish | 2027-04-12 |
| v1.0.0 Release and Donations | 2027-05-03 |

## Bootstrap

```powershell
pwsh tools/bootstrap/Initialize-GitHubProject.ps1
```

The script is idempotent:

- Creates or reuses the project.
- Links the repository.
- Updates the Status options.
- Creates missing single-select, date, and iteration fields.
- Creates missing milestones and labels.
- Creates missing views.
- Adds all open issues and pull requests and synchronizes their fields.

## Item synchronization

`tools/agent/sync-project-item.ps1` adds an issue or pull request to the project and updates:

- Status from state, labels, and review state.
- Priority from `priority:P0` through `priority:P3`.
- Area from `area:*` labels.
- Size from `size:S`, `size:M`, or `size:L`.
- Phase from the milestone title.
- Target from the milestone due date.
- Current sprint from the iteration field, when a sprint is active.

Example:

```powershell
pwsh tools/agent/sync-project-item.ps1 -ItemUrl https://github.com/jb-coder/JabaSurvivol/issues/3
```

## Automation

The `project-automation` workflow runs on issue, pull request, and pull request review events. It uses `PROJECT_TOKEN` and calls the synchronization script.

`PROJECT_TOKEN` must have the `project` scope. It is stored only as a GitHub Actions secret. Rotate it if the GitHub CLI session changes.

GitHub does not expose creation of built-in Project workflows through the public API. The repository uses the custom `project-automation` workflow for field synchronization. Built-in workflows can be enabled manually from the Project UI if additional status transitions are desired.

## Recommended built-in workflows

If enabled manually in the Project UI:

- Item closed -> Done.
- Pull request merged -> Done.
- Item reopened -> Ready.
- Code review approved -> In Review.
- Code changes requested -> Changes Requested.

The custom workflow already handles these states, so built-in workflows are optional.

## Current items

- Issue #3: Unity walking skeleton, Ready, P1, Game, L, Walking Skeleton, Sprint 1.
- Pull request #1: Actions dependency update, In Review, P2, CI, S, Sprint 1.
- Pull request #2: .NET dependency update, In Review, P2, CI, M, Sprint 1.
