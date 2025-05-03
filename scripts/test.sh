#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SLN_FILE="${REPO_ROOT}/TemporaryName.sln"
CONFIG="Release"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${SLN_FILE}" ] || log_error "Solution file not found: ${SLN_FILE}"

echo "Running tests for solution: ${SLN_FILE} (Using ${CONFIG} build)"
if ! dotnet test "${SLN_FILE}" -c "${CONFIG}" --no-build --verbosity normal; then
    log_error "Tests failed. Check output above."
fi
echo "Tests passed."