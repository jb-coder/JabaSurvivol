# Deployment Runbook

> Historical Oracle/k3s runbook. The beta runs locally over Tailscale, and the
> eventual hosting target is a VPS at final handoff. No deployment is performed
> during v0.5.1. See ADR-0008.

## Prerequisites

- Oracle Always Free A1 VM (2 OCPU, 12 GB) with Ubuntu 24.04 ARM64.
- Single-node k3s with Traefik, cert-manager, metrics-server, and local-path.
- Namespace `jabasurvivol` and the `jabasurvivol-secrets` Secret from SOPS.
- GHCR pull secret `ghcr-pull-secret` in the namespace.
- GitHub environment `production` with required reviewers.
- Repository variable `DEPLOY_ENABLED=true`.
- Repository or environment secret `KUBE_CONFIG_B64` containing a base64
  kubeconfig with least-privilege namespace access.

## Deploy

1. Dispatch `deploy-api` with the semantic version or push a `v*` tag from
   `main`.
2. The workflow tests the solution and builds immutable ARM64 images tagged
   with the commit SHA and semantic version.
3. Support manifests are applied, PostgreSQL is waited on, and a pre-migration
   `pg_dump` is written to the in-cluster `postgres-backup` PVC.
4. `db-migrate` runs the Identity and Game/Telemetry migrations in order.
5. Application manifests are applied and every Deployment is pinned to the
   image digest.
6. `/health`, `/alive`, and `/api/v1/status` are checked through the BFF.
7. Any failure after the backup triggers a rollout undo for all deployments.

## Manual rollback

```text
kubectl -n jabasurvivol rollout undo deployment/bff
kubectl -n jabasurvivol rollout undo deployment/game-api
kubectl -n jabasurvivol rollout undo deployment/identity-api
kubectl -n jabasurvivol rollout undo deployment/telemetry-worker
kubectl -n jabasurvivol rollout status deployment/bff --timeout=120s
```

Migrations are additive. Do not reverse a schema change in production. Restore
the pre-migration dump into a scratch database first, verify row counts, then
follow the approved restore procedure.

## Migration failure

1. Inspect `kubectl -n jabasurvivol logs job/db-migrate --all-containers=true`.
2. Confirm the backup artifact exists and is non-empty.
3. Fix forward with an additive migration when possible.
4. If a manual restore is required, restore into a scratch database, verify the
   schema version and row counts, then restore production under the approved
   runbook.
5. Open an incident issue with the failing migration ID and evidence.

## Certificate failure

1. Check `kubectl -n jabasurvivol describe certificate`.
2. Confirm DNS points at the Traefik service and port 443 is reachable.
3. Check cert-manager logs and ACME challenge records.
4. Renew after fixing DNS or challenge configuration; do not disable TLS
   verification.

## RPO and RTO

- RPO: up to 24 hours from the nightly backup; the deploy workflow also creates
  a pre-migration backup.
- RTO: target 2 hours for a single-node restore.
- Backups retain 7 daily and 4 weekly copies once the Oracle provisioning issue
  is complete.

## Security

- Never print kubeconfig, connection strings, JWT keys, or pod environment.
- The workflow does not print kubeconfig, connection strings, JWT keys, or pod
  environment. Migration failure logs may include EF migration IDs and table
  names, but no secret values.
- Rotate the JWT signing key and database password through the SOPS workflow.
