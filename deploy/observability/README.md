# Observability

Planned dashboards and alerts:

- API request rate, error rate, and p95 latency.
- Pod restarts and resource usage.
- Outbox lag and queue depth.
- PostgreSQL connections and storage.
- Certificate expiry.
- Backup success and restore verification.
- Uptime checks for the BFF.

Telemetry is exported through OpenTelemetry to Grafana Cloud Free or a self-hosted collector.
