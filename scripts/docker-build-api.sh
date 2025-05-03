#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

IMAGE_NAME="${1:-temporaryname-webapi}"
TAG="${2:-latest}"
DOCKERFILE_PATH="${REPO_ROOT}/src/TemporaryName.WebApi/Dockerfile"
BUILD_CONTEXT="${REPO_ROOT}"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v docker &> /dev/null || log_error "'docker' command not found. Ensure Docker is installed and running."
[ -f "${DOCKERFILE_PATH}" ] || log_error "Dockerfile not found: ${DOCKERFILE_PATH}"
[ -d "${BUILD_CONTEXT}" ] || log_error "Build context directory not found: ${BUILD_CONTEXT}"

echo "Building Docker image '${IMAGE_NAME}:${TAG}' from Dockerfile: ${DOCKERFILE_PATH}"
if ! docker build -t "${IMAGE_NAME}:${TAG}" -f "${DOCKERFILE_PATH}" "${BUILD_CONTEXT}"; then
    log_error "Docker build failed."
fi
echo "Docker image '${IMAGE_NAME}:${TAG}' built successfully."