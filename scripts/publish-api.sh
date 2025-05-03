#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
API_PROJECT_FILE="${REPO_ROOT}/src/TemporaryName.WebApi/TemporaryName.WebApi.csproj"
OUTPUT_DIR="${REPO_ROOT}/publish/webapi"
CONFIG="Release"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${API_PROJECT_FILE}" ] || log_error "API project file not found: ${API_PROJECT_FILE}"

echo "Publishing Web API project: ${API_PROJECT_FILE}"
echo "Configuration: ${CONFIG}"
echo "Output directory: ${OUTPUT_DIR}"

# Ensure output directory exists
mkdir -p "${OUTPUT_DIR}" || log_error "Failed to create output directory: ${OUTPUT_DIR}"

# Publish the project
if ! dotnet publish "${API_PROJECT_FILE}" -c "${CONFIG}" -o "${OUTPUT_DIR}" --no-build; then
    log_error "Publishing failed."
fi

echo "Publishing complete."