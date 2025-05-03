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
# Check if EF tool is available (simple check, might need specific version check)
dotnet ef --version &> /dev/null || log_error "'dotnet ef' command not found or failed. Ensure EF Core tools are installed (dotnet tool install --global dotnet-ef)."

if [ -z "${1:-}" ]; then
  log_error "Migration name is required. Usage: $0 <YourMigrationName>"
fi
MIGRATION_NAME="$1"

[ -d "${PERSISTENCE_PROJECT}" ] || log_error "Persistence project directory not found: ${PERSISTENCE_PROJECT}"
[ -d "${STARTUP_PROJECT}" ] || log_error "Migrations tool project directory not found: ${STARTUP_PROJECT}"

echo "Adding migration '${MIGRATION_NAME}'..."
if ! dotnet ef migrations add "${MIGRATION_NAME}" --project "${PERSISTENCE_PROJECT}" --startup-project "${STARTUP_PROJECT}"; then
    log_error "Failed to add migration."
fi
echo "Migration added successfully."