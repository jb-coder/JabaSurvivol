---
name: oracle-k3s
description: Use when deploying or operating JabaSurvivol on Oracle Always Free with k3s, Traefik, cert-manager, SOPS, GHCR, PostgreSQL, and OpenTelemetry.
---

# Oracle Always Free and k3s

## Budget

The Always Free A1 allowance is budgeted at 2 OCPU and 12 GB RAM for the tenancy. Do not assume 4 OCPU and 24 GB. Capacity can be unavailable; retry in supported availability domains or use a documented fallback.

## Cluster baseline

- Ubuntu 24.04 ARM64.
- k3s single node.
- Traefik ingress.
- cert-manager with Let's Encrypt.
- local-path storage.
- Namespaces: `jabasurvivol`, `observability`.
- Resource requests and limits for every workload.
- One replica per service for the MVP.

## Workloads

- PostgreSQL StatefulSet with a persistent volume.
- RabbitMQ StatefulSet when messaging is introduced.
- Game API, Identity API, Telemetry Worker, and BFF.
- Only the BFF is exposed through ingress.
- Internal services are `ClusterIP`.

## Deployment

- Images from GHCR, tagged with commit SHA and semantic version.
- Deploy by digest when possible.
- Migrations run as a Kubernetes Job, not at application startup.
- Back up before migrations.
- Rolling update with readiness and liveness probes.
- Smoke test after deploy.
- Automatic rollback on failure.

## Secrets

- SOPS with age for encrypted secrets in Git.
- The age private key lives in GitHub Actions secrets.
- No plaintext secrets in the repository.
- Rotate credentials regularly.

## Observability

- OpenTelemetry to Grafana Cloud Free or a self-hosted collector.
- Dashboards for requests, errors, latency, outbox lag, queue depth, and resource usage.
- Alerts for error rate, p95 latency, pod restarts, disk usage, certificate expiry, and backup failure.
- Uptime checks for the BFF.

## Backups

- Nightly `pg_dump` to Oracle Object Storage or Cloudflare R2.
- Retention: 7 daily, 4 weekly.
- Weekly restore verification.
- Document RPO and RTO.

## Security

- SSH key-only, no root login.
- Firewall only ports 22, 80, and 443.
- Kubernetes RBAC least privilege.
- Network policies.
- Non-root containers, read-only root filesystem where possible.
- Image scanning with Trivy.
- No paid resources.
