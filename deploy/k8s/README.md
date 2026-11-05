# Kubernetes Manifests

Base manifests and Kustomize overlays for JabaSurvivol.

Planned layout:

```text
base/
  namespace.yaml
  bff.yaml
  game-api.yaml
  identity-api.yaml
  telemetry-worker.yaml
  postgres.yaml
  rabbitmq.yaml
overlays/
  prod/
    kustomization.yaml
    ingress.yaml
    resource-patch.yaml
```

Rules:

- Only the BFF is exposed through ingress.
- Every workload has requests and limits.
- Every container runs as non-root.
- Migrations run as a Kubernetes Job.
- Health checks use `/health` and `/alive`.
