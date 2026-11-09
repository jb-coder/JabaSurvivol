#!/usr/bin/env bash
set -euo pipefail

KUBECTL="${KUBECTL:-kubectl}"

for deployment in bff game-api identity-api telemetry-worker; do
  "$KUBECTL" -n jabasurvivol rollout undo "deployment/${deployment}" || true
done

"$KUBECTL" -n jabasurvivol rollout status deployment/bff --timeout=120s || true
"$KUBECTL" -n jabasurvivol get pods -o wide || true
