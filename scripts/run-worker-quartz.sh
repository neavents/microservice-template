#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
WORKER_PROJECT_FILE="${REPO_ROOT}/src/TemporaryName.Worker.Quartz/TemporaryName.Worker.Quartz.csproj"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${WORKER_PROJECT_FILE}" ] || log_error "Quartz worker project file not found: ${WORKER_PROJECT_FILE}"

echo "Running Quartz Worker project: ${WORKER_PROJECT_FILE}"
if ! dotnet run --project "${WORKER_PROJECT_FILE}"; then
    log_error "Failed to run Quartz worker project."
fi