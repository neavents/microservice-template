#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PERSISTENCE_PROJECT="${REPO_ROOT}/src/TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL"
STARTUP_PROJECT="${REPO_ROOT}/tools/TemporaryName.Tools.Persistence.Migrations"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
dotnet ef --version &> /dev/null || log_error "'dotnet ef' command not found or failed. Ensure EF Core tools are installed."

[ -d "${PERSISTENCE_PROJECT}" ] || log_error "Persistence project directory not found: ${PERSISTENCE_PROJECT}"
[ -d "${STARTUP_PROJECT}" ] || log_error "Migrations tool project directory not found: ${STARTUP_PROJECT}"

echo "Applying database migrations..."
if ! dotnet ef database update --project "${PERSISTENCE_PROJECT}" --startup-project "${STARTUP_PROJECT}"; then
    log_error "Failed to apply migrations."
fi
echo "Migrations applied successfully."