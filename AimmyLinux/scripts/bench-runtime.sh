#!/usr/bin/env bash
set -euo pipefail

CONFIG_PATH="${1:-aimmylinux.json}"
DURATION_SECONDS="${2:-180}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Running runtime benchmark for ${DURATION_SECONDS}s using ${CONFIG_PATH}"

cd "$ROOT_DIR"
timeout "${DURATION_SECONDS}"s dotnet run -c Release -- --config "${CONFIG_PATH}" | tee "bench-output.log"

echo "Benchmark log written to bench-output.log"
