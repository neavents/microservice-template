#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DOCKER_COMPOSE_FILE="${REPO_ROOT}/docker-compose.yml"
SERVICE_NAME="${1:-}" # Optional service name argument

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

check_docker_compose() {
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    elif docker compose version &> /dev/null; then
        COMPOSE_CMD="docker compose"
    else
        log_error "'docker-compose' or 'docker compose' command not found. Ensure Docker Compose is installed."
    fi
}

command -v docker &> /dev/null || log_error "'docker' command not found."
check_docker_compose
[ -f "${DOCKER_COMPOSE_FILE}" ] || log_error "Docker Compose file not found: ${DOCKER_COMPOSE_FILE}"

if [ -z "${SERVICE_NAME}" ]; then
  echo "Tailing logs for all services defined in ${DOCKER_COMPOSE_FILE}..."
  ${COMPOSE_CMD} -f "${DOCKER_COMPOSE_FILE}" logs --follow
else
  echo "Tailing logs for service '${SERVICE_NAME}' from ${DOCKER_COMPOSE_FILE}..."
  ${COMPOSE_CMD} -f "${DOCKER_COMPOSE_FILE}" logs --follow "${SERVICE_NAME}"
fi

# Note: This script will run indefinitely until interrupted (Ctrl+C).
# Error handling for the logs command itself is tricky as it's meant to stream.