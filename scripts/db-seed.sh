#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SEEDING_TOOL_PROJECT_FILE="${REPO_ROOT}/tools/TemporaryName.Tools.Persistence.Seeding/TemporaryName.Tools.Persistence.Seeding.csproj"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
[ -f "${SEEDING_TOOL_PROJECT_FILE}" ] || log_error "Seeding tool project file not found: ${SEEDING_TOOL_PROJECT_FILE}"

echo "Running database seeder: ${SEEDING_TOOL_PROJECT_FILE}"
if ! dotnet run --project "${SEEDING_TOOL_PROJECT_FILE}"; then
    log_error "Database seeding failed."
fi
echo "Seeding complete."