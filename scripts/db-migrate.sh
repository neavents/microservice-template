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

echo "Running C# Migrations Tool to apply migrations..."
echo "  Tool Project: ${MIGRATIONS_TOOL_PROJECT_RELPATH}"
echo "  Persistence Project: ${PERSISTENCE_PROJECT_RELPATH}"
echo "  Database Type: ${DB_TYPE}"

TOOL_ARGS=(
    "apply"
    "-p" "${PERSISTENCE_PROJECT_ABS}"
    "-t" "${DB_TYPE}"
)

if ! dotnet run --project "${MIGRATIONS_TOOL_PROJECT_ABS}" -- "${TOOL_ARGS[@]}"; then
    log_error "Failed to apply migrations using the C# tool. Check tool logs and database connection."
fi

echo "Migrations applied successfully via C# tool."