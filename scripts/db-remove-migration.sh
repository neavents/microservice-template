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

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."

[ -d "${PERSISTENCE_PROJECT_ABS}" ] || log_error "Persistence project directory not found: ${PERSISTENCE_PROJECT_ABS}"
[ -d "${MIGRATIONS_TOOL_PROJECT_ABS}" ] || log_error "Migrations tool project directory not found: ${MIGRATIONS_TOOL_PROJECT_ABS}"

echo "Running C# Migrations Tool to remove the last migration..."
echo "  Tool Project: ${MIGRATIONS_TOOL_PROJECT_RELPATH}"
echo "  Persistence Project: ${PERSISTENCE_PROJECT_RELPATH}"
echo "  Database Type: ${DB_TYPE}"

if ! dotnet run --project "${MIGRATIONS_TOOL_PROJECT_ABS}" -- \
     remove \
     -p "${PERSISTENCE_PROJECT_ABS}" \
     -t "${DB_TYPE}"; then
    log_error "Failed to remove migration using the C# tool. Check tool logs for details."
fi

echo "Last migration removed successfully via C# tool."