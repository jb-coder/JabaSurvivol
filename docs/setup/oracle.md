# Oracle Always Free Setup

## Budget

Plan for 2 OCPU and 12 GB RAM on the A1 allowance. Some tenancies or regions may show a larger allowance, but the project must fit the lower budget.

Always Free resources used:

- One Ampere A1 VM with 2 OCPU and 12 GB RAM.
- 200 GB combined block volume.
- 20 GB object storage.
- 50,000 object storage API requests per month.
- Outbound data transfer within the Always Free allowance.

Do not create paid resources. Set budget alerts even on a free account.

## Region and capacity

- Use the home region.
- A1 capacity can be unavailable; retry in supported availability domains.
- If capacity is unavailable, document a fallback before creating anything else.

## Base image

- Ubuntu 24.04 ARM64.
- Key-only SSH.
- No root login.
- Firewall: only 22, 80, and 443.
- Automatic security updates.

## k3s

Install a single-node k3s cluster.

Components:

- Traefik ingress.
- cert-manager with Let's Encrypt.
- local-path storage.
- Metrics server.
- Namespaces: `jabasurvivol`, `observability`.

## Workloads

- PostgreSQL StatefulSet.
- RabbitMQ StatefulSet when messaging is introduced.
- Game API.
- Identity API.
- Telemetry Worker.
- BFF.
- Only the BFF is exposed through ingress.

## Resource planning

- k3s and system: about 1 GB.
- Traefik and cert-manager: about 200 MB.
- PostgreSQL: 1-2 GB.
- RabbitMQ: 512 MB-1 GB.
- Services: 1.5-2 GB total.
- Observability: 256-512 MB.
- Leave headroom for the OS and bursts.

Set requests and limits for every workload. Use one replica per service in the MVP.

## Secrets

- SOPS with age.
- Encrypted secrets committed under `deploy/secrets`.
- Age private key stored in GitHub Actions secrets.
- No plaintext secrets in Git.

## Deployment

- Images from GHCR.
- Immutable tags by commit SHA and semantic version.
- Deploy by digest when possible.
- Migrations run as a Kubernetes Job.
- Backup before migrations.
- Rolling update with readiness and liveness probes.
- Smoke test after deploy.
- Automatic rollback on failure.

## Backups

- Nightly `pg_dump` to Oracle Object Storage or Cloudflare R2.
- 7 daily and 4 weekly backups.
- Weekly restore verification.
- Document RPO and RTO.

## Observability

- OpenTelemetry to Grafana Cloud Free.
- Dashboards for requests, errors, latency, queue depth, and resources.
- Alerts for error rate, p95 latency, pod restarts, disk usage, certificate expiry, and backup failure.
- Uptime checks for the BFF.

## Bootstrap checklist

1. Create the Oracle account and verify the tenancy.
2. Create the A1 VM in the home region.
3. Configure SSH and firewall.
4. Install k3s.
5. Install Traefik, cert-manager, and metrics server.
6. Create namespaces and resource quotas.
7. Install PostgreSQL.
8. Configure GHCR pull secret.
9. Configure SOPS and age.
10. Configure GitHub Actions secrets.
11. Set `DEPLOY_ENABLED=true`.
12. Verify health checks, backups, and rollback.
