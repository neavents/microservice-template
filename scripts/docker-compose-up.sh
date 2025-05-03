#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DOCKER_COMPOSE_FILE="${REPO_ROOT}/docker-compose.yml"

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

command -v docker &> /dev/null || log_error "'docker' command not found. Ensure Docker is installed and running."
check_docker_compose
[ -f "${DOCKER_COMPOSE_FILE}" ] || log_error "Docker Compose file not found: ${DOCKER_COMPOSE_FILE}"

echo "Starting Docker Compose environment using file: ${DOCKER_COMPOSE_FILE}"
# Add --wait for synchronous startup if needed, but -d is common for background start
if ! ${COMPOSE_CMD} -f "${DOCKER_COMPOSE_FILE}" up -d --remove-orphans --build; then
    log_error "Docker Compose up command failed."
fi
echo "Docker Compose environment starting in detached mode."