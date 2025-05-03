#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONFIG="Release"

PUBLISH_ITEMS=(
  "src/TemporaryName.WebApi:webapi"
  "src/TemporaryName.Worker.Hangfire:worker-hangfire"
  "src/TemporaryName.Worker.Quartz:worker-quartz"
)

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."

echo "Publishing all applications (Configuration: ${CONFIG})..."

# First, build the entire solution once to ensure dependencies are met
SLN_FILE="${REPO_ROOT}/TemporaryName.sln"
[ -f "${SLN_FILE}" ] || log_error "Solution file not found: ${SLN_FILE}"
echo "Performing initial solution build..."
if ! dotnet build "${SLN_FILE}" -c "${CONFIG}"; then
  log_error "Initial solution build failed. Cannot proceed with publishing."
fi
echo "Initial build successful."


for item in "${PUBLISH_ITEMS[@]}"; do
  IFS=':' read -r project_path output_suffix <<< "$item"
  PROJECT_FILE="${REPO_ROOT}/${project_path}/${project_path##*/}.csproj"
  OUTPUT_DIR="${REPO_ROOT}/publish/${output_suffix}"

  [ -f "${PROJECT_FILE}" ] || { echo "WARNING: Project file not found for ${project_path}, skipping." >&2; continue; }

  echo "--------------------------------------------------"
  echo "Publishing project: ${PROJECT_FILE}"
  echo "Output directory: ${OUTPUT_DIR}"
  echo "--------------------------------------------------"

  mkdir -p "${OUTPUT_DIR}" || log_error "Failed to create output directory: ${OUTPUT_DIR}"

  if ! dotnet publish "${PROJECT_FILE}" -c "${CONFIG}" -o "${OUTPUT_DIR}" --no-build; then
      log_error "Publishing failed for project: ${PROJECT_FILE}"
  fi
  echo "Publishing complete for: ${PROJECT_FILE}"
done

echo "--------------------------------------------------"
echo "All specified applications published successfully."
echo "--------------------------------------------------"