#!/usr/bin/env bash
set -euo pipefail

KUBECTL="${KUBECTL:-kubectl}"

for deployment in bff game-api identity-api telemetry-worker; do
  "$KUBECTL" -n jabasurvivol rollout undo "deployment/${deployment}"
done

for deployment in bff game-api identity-api telemetry-worker; do
  "$KUBECTL" -n jabasurvivol rollout status "deployment/${deployment}" --timeout=120s
done

"$KUBECTL" -n jabasurvivol get pods -o wide || true
