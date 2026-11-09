#!/usr/bin/env bash
set -euo pipefail
echo "$*" >> "${ROLLBACK_LOG:?ROLLBACK_LOG is required}"
