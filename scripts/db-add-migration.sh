#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

PERSISTENCE_PROJECT_RELPATH="src/TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL"
MIGRATIONS_TOOL_PROJECT_RELPATH="tools/TemporaryName.Tools.Persistence.Migrations"
DB_TYPE="postgresql"

PERSISTENCE_PROJECT_ABS="${REPO_ROOT}/${PERSISTENCE_PROJECT_RELPATH}"
MIGRATIONS_TOOL_PROJECT_ABS="${REPO_ROOT}/${MIGRATIONS_TOOL_PROJECT_RELPATH}"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found. Make sure the .NET SDK is installed and in your PATH."

if [ -z "${1:-}" ]; then
  log_error "Migration name is required. Usage: $0 <YourMigrationName>"
fi
MIGRATION_NAME="$1"

[ -d "${PERSISTENCE_PROJECT_ABS}" ] || log_error "Persistence project directory not found: ${PERSISTENCE_PROJECT_ABS}"
[ -d "${MIGRATIONS_TOOL_PROJECT_ABS}" ] || log_error "Migrations tool project directory not found: ${MIGRATIONS_TOOL_PROJECT_ABS}"

echo "Running C# Migrations Tool to add migration '${MIGRATION_NAME}'..."
echo "  Tool Project: ${MIGRATIONS_TOOL_PROJECT_RELPATH}"
echo "  Persistence Project: ${PERSISTENCE_PROJECT_RELPATH}"
echo "  Database Type: ${DB_TYPE}"

if ! dotnet run --project "${MIGRATIONS_TOOL_PROJECT_ABS}" -- \
     add "${MIGRATION_NAME}" \
     -p "${PERSISTENCE_PROJECT_ABS}" \
     -t "${DB_TYPE}"; then
    log_error "Failed to add migration using the C# tool. Check tool logs for details."
fi

echo "Migration '${MIGRATION_NAME}' added successfully via C# tool."