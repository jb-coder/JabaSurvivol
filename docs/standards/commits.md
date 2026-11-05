# Commit Standards

## Format

Use Conventional Commits in English:

```text
<type>(<scope>): <imperative summary>
```

Allowed types:

- `feat`
- `fix`
- `refactor`
- `test`
- `docs`
- `chore`
- `perf`
- `build`
- `ci`

Suggested scopes:

- `game`
- `core`
- `api`
- `identity`
- `telemetry`
- `bff`
- `unity`
- `android`
- `ci`
- `docs`
- `agents`

Examples:

```text
feat(game): add persistent supply points
fix(core): clamp armor absorption
test(core): cover skill point allocation
docs(adr): record autonomous review workflow
chore(ci): cache the Unity Library folder
```

## Rules

- Use the imperative mood.
- Keep the subject at or below 72 characters.
- Explain the reason in the body when it is not obvious.
- One logical change per commit.
- Do not commit generated build output, secrets, or local configuration.
- Do not mention AI, assistants, LLMs, generation tools, or agent frameworks.
- Do not add tool co-author trailers.
- Do not use emojis.

## Enforcement

- The PR title must follow the same convention.
- The `pr-policy` workflow rejects invalid titles and forbidden attribution phrases.
- The reviewer rejects commits that violate this standard.
