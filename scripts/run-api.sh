#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
API_PROJECT_FILE="${REPO_ROOT}/src/TemporaryName.WebApi/TemporaryName.WebApi.csproj"
LAUNCH_PROFILE="https" 

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${API_PROJECT_FILE}" ] || log_error "API project file not found: ${API_PROJECT_FILE}"

echo "Running Web API project: ${API_PROJECT_FILE}"
echo "Using launch profile: ${LAUNCH_PROFILE}"

if [ -n "${LAUNCH_PROFILE}" ]; then
    if ! dotnet run --project "${API_PROJECT_FILE}" --launch-profile "${LAUNCH_PROFILE}"; then
        log_error "Failed to run Web API project."
    fi
else
     if ! dotnet run --project "${API_PROJECT_FILE}"; then
        log_error "Failed to run Web API project."
    fi
fi