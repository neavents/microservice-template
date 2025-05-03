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

command -v dotnet &> /dev/null || log_error "'dotnet' command not found. Ensure .NET SDK is installed and in PATH."
[ -f "${SLN_FILE}" ] || log_error "Solution file not found: ${SLN_FILE}"

echo "Building solution: ${SLN_FILE} (Configuration: ${CONFIG})"
if ! dotnet build "${SLN_FILE}" -c "${CONFIG}"; then
  log_error "Build failed."
fi
echo "Build complete."