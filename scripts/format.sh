#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

log_error() {
  echo "ERROR: $1" >&2
  exit 1
}

log_warning() {
    echo "WARNING: $1" >&2
}

check_tool() {
    local tool_name=$1
    local install_cmd=$2
    local dotnet_tool_cmd="dotnet tool run ${tool_name}"
    local direct_cmd="${tool_name}"

    if command -v "${dotnet_tool_cmd%% *}" &> /dev/null && ${dotnet_tool_cmd} --version &> /dev/null; then
        TOOL_CMD="${dotnet_tool_cmd}"
        return 0
    elif command -v "${direct_cmd}" &> /dev/null && ${direct_cmd} --version &> /dev/null; then
        TOOL_CMD="${direct_cmd}"
        return 0
    else
        log_warning "${tool_name} command not found or failed to execute."
        log_warning "Attempt installing/restoring via: ${install_cmd}"
        log_warning "Or ensure it's installed globally and in PATH."
        # Optionally attempt install:
        # echo "Attempting to install ${tool_name}..."
        # if ! ${install_cmd}; then log_error "Failed to install ${tool_name}."; fi
        # check_tool "$1" "$2" # Recurse after install attempt
        log_error "${tool_name} is required but not available."
    fi
}

command -v dotnet &> /dev/null || log_error "'dotnet' command not found."
check_tool "csharpier" "dotnet tool restore" # Assuming it's in dotnet-tools.json

echo "Formatting code in repository root: ${REPO_ROOT}"
if ! ${TOOL_CMD} .; then
  log_error "Formatting command failed."
fi
echo "Formatting complete."