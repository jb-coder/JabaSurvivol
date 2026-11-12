# Oracle Infrastructure

> Historical. Oracle is out of scope (ADR-0008). The beta runs locally over
> Tailscale, and the eventual hosting target is a VPS at final handoff. These
> notes are retained for reference only.

Bootstrap and operational notes for Oracle Always Free.

Budget: 2 OCPU and 12 GB RAM for the A1 allowance.

Planned scripts:

- VM bootstrap and hardening.
- k3s installation.
- Traefik and cert-manager setup.
- PostgreSQL and RabbitMQ installation.
- SOPS and age setup.
- Backup and restore verification.
- Smoke test and rollback.

See `docs/setup/oracle.md` for the full checklist.
