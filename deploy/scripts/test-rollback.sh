#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
log="$(mktemp)"
trap 'rm -f "$log"' EXIT

KUBECTL="$root/deploy/scripts/kubectl-mock.sh" ROLLBACK_LOG="$log" \
  bash "$root/deploy/scripts/rollback.sh"

for deployment in bff game-api identity-api telemetry-worker; do
  grep -q "rollout undo deployment/${deployment}" "$log"
done
grep -q "rollout status deployment/bff" "$log"
