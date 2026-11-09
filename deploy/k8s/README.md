# Kubernetes Manifests

Base manifests and Kustomize overlays for JabaSurvivol on a single-node k3s
cluster. Only the BFF is exposed through ingress.

## Layout

```text
base/
  namespace.yaml
  serviceaccount.yaml
  configmap.yaml
  identity-api.yaml
  game-api.yaml
  bff.yaml
  telemetry-worker.yaml
  postgres.yaml
  network-policies.yaml
overlays/
  prod/
    kustomization.yaml
    ingress.yaml
```

## Prerequisites

Provision the cluster with `docs/setup/oracle.md`: one Oracle Always Free A1
VM (2 OCPU, 12 GB), k3s with Traefik, cert-manager, local-path storage,
metrics-server, SOPS/age, and a GHCR pull secret named `ghcr-pull-secret` in the
`jabasurvivol` namespace. The ServiceAccount references it.

## Secret contract

Create the `jabasurvivol-secrets` Secret through SOPS or the deployment
pipeline. Never commit plaintext values. Required keys:

- `database-connection-string`: Npgsql connection string for the shared
  `jabasurvivol` database. Each module owns its schema.
- `jwt-signing-key`: at least 32 characters, shared by Identity, Game, and BFF.
- `postgres-password`: password for the `postgres` user.

```text
kubectl -n jabasurvivol create secret generic jabasurvivol-secrets \
  --from-literal=database-connection-string=... \
  --from-literal=jwt-signing-key=... \
  --from-literal=postgres-password=...
```

In production this Secret is rendered from SOPS by the deploy workflow. The
repository contains no plaintext secret.

## Resource budget

The manifests fit the Oracle Always Free 2 OCPU / 12 GB A1 budget:

- Identity API: 100m-300m CPU, 256-512 MiB.
- Game API: 150m-500m CPU, 384-768 MiB.
- BFF: 100m-250m CPU, 192-384 MiB.
- Telemetry worker: 100m-250m CPU, 192-384 MiB.
- PostgreSQL: 250m-600m CPU, 1-2 GiB, 10 GiB local-path volume.

The limits total 1.9 OCPU, leaving headroom below the 2 OCPU budget.

One replica per workload. Horizontal scaling requires an ADR.

## Validation

```text
kubectl kustomize deploy/k8s/overlays/prod > rendered.yaml
docker run --rm -v "$PWD/rendered.yaml:/manifests/rendered.yaml" \
  ghcr.io/yannh/kubeconform:v0.6.7 -strict -summary /manifests/rendered.yaml
```

`container-ci` builds and scans all four images with Trivy and validates the
rendered manifests.

## Rollback

```text
kubectl -n jabasurvivol rollout undo deployment/bff
kubectl -n jabasurvivol rollout undo deployment/game-api
kubectl -n jabasurvivol rollout undo deployment/identity-api
kubectl -n jabasurvivol rollout undo deployment/telemetry-worker
```

Migrations are additive. Restore the pre-migration backup before rolling back a
schema change.

## Ingress

The prod overlay exposes only `bff` through Traefik. The host is a placeholder
(`api.jabasurvivol.example.com`) until the Oracle and DNS provisioning issue
supplies the real host.
