# Incident Runbook

## Principles

1. Protect player data and availability first.
2. Roll back before diagnosing.
3. Do not run destructive commands.
4. Record evidence.
5. Open an incident issue.

## Backend outage

1. Check `/health` and `/alive`.
2. Check Kubernetes pod status, restarts, and events.
3. Check recent deploys and database migrations.
4. If the issue started after a deploy, roll back to the previous image.
5. Verify health and smoke tests.
6. Open an incident issue with timeline, impact, and rollback digest.

## Failed migration

1. Stop the rollout.
2. Verify the pre-migration backup.
3. Roll back the application image.
4. Restore only if data is corrupted and a verified backup exists.
5. Document the migration failure and add a regression test.

## Data corruption

1. Freeze writes if possible.
2. Identify the last known good backup.
3. Restore to a scratch database first.
4. Verify integrity.
5. Restore production only after evidence is recorded.

## Unity or APK regression

1. Identify the last good release tag.
2. Reproduce on the same device or emulator.
3. Attach `logcat` and screenshots.
4. Fix on a `hotfix/*` branch.
5. Release a patch and verify the APK.

## Security incident

1. Revoke exposed credentials.
2. Rotate affected secrets.
3. Preserve logs.
4. Assess data exposure.
5. Patch and release.
6. Document the incident without exposing secrets.

## Communication

- Use the incident issue for the timeline.
- Do not mention AI, assistants, or generation tools.
- Do not claim manual testing that did not happen.
