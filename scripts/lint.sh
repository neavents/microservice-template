#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SLN_FILE="${REPO_ROOT}/TemporaryName.sln"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${SLN_FILE}" ] || log_error "Solution file not found: ${SLN_FILE}"

echo "Linting code (checking format and running analyzers)..."
if ! dotnet format "${SLN_FILE}" --verify-no-changes; then
    log_error "Formatting check failed. Run './scripts/format.sh' to fix."
fi

# echo "Performing strict build analysis..."
# if ! dotnet build "${SLN_FILE}" -c Release /warnaserror; then
#     log_error "Build analysis failed with warnings treated as errors."
# fi

echo "Linting checks passed."