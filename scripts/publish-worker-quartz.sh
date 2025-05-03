#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
WORKER_PROJECT_FILE="${REPO_ROOT}/src/TemporaryName.Worker.Quartz/TemporaryName.Worker.Quartz.csproj"
OUTPUT_DIR="${REPO_ROOT}/publish/worker-quartz"
CONFIG="Release"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${WORKER_PROJECT_FILE}" ] || log_error "Worker project file not found: ${WORKER_PROJECT_FILE}"

echo "Publishing Quartz worker project: ${WORKER_PROJECT_FILE}"
echo "Configuration: ${CONFIG}"
echo "Output directory: ${OUTPUT_DIR}"

mkdir -p "${OUTPUT_DIR}" || log_error "Failed to create output directory: ${OUTPUT_DIR}"

if ! dotnet publish "${WORKER_PROJECT_FILE}" -c "${CONFIG}" -o "${OUTPUT_DIR}" --no-build; then
    log_error "Publishing failed."
fi

echo "Quartz Worker publishing complete."