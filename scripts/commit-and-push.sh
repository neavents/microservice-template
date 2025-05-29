#!/bin/bash

set -euo pipefail 

# --- Configuration ---

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
STARTING_PATH="$(pwd)" 


log_error() {
  echo "ERROR: $1" >&2

}

cleanup_and_exit() {
    local exit_code=$? 
    echo "Returning to original path: ${STARTING_PATH}"
    cd "${STARTING_PATH}" || echo "Warning: Could not return to starting path ${STARTING_PATH}"
    # if [ ${exit_code} -ne 0 ]; then
    #    echo "Script finished with errors."
    # fi
    exit ${exit_code}
}

trap cleanup_and_exit EXIT

# --- Argument Check ---
if [ -z "${1:-}" ]; then
  log_error "Commit message is required."
  log_error "Usage: $0 \"Your commit message\""
  exit 1 
fi
COMMIT_MESSAGE="$1"

# --- Pre-flight Checks ---
command -v git &> /dev/null || { log_error "'git' command not found. Ensure Git is installed and in your PATH."; exit 1; }


if [ "$(pwd)" != "${REPO_ROOT}" ]; then
  echo "Changing to repository root: ${REPO_ROOT}"
  cd "${REPO_ROOT}" || { log_error "Failed to change directory to repository root: ${REPO_ROOT}"; exit 1; }
fi


if ! git rev-parse --is-inside-work-tree &>/dev/null; then
    log_error "Not a git repository: $(pwd)"
    exit 1
fi

echo "Current directory: $(pwd)"
echo "Commit message: \"${COMMIT_MESSAGE}\""

# --- Git Operations ---
echo "Adding all changes..."
if ! git add .; then
  log_error "git add . failed."
  exit 1 
fi
echo "All changes added."

echo "Committing changes..."
if ! git commit -m "${COMMIT_MESSAGE}"; then
  # Check if commit failed because there was nothing to commit
  if git status --porcelain | grep -q '^??'; then 
      echo "Commit failed. There might be untracked files or other issues."
  elif git status --porcelain | grep -q -v '^??'; then 
      echo "Warning: Commit command executed, but there might have been nothing to commit or only untracked files present."
  else 
      echo "No changes to commit."
      exit 0
  fi

fi
echo "Changes committed."

echo "Pushing changes to remote..."
if ! git push; then
  log_error "git push failed."
  exit 1 
fi
echo "Changes pushed successfully."

echo "Commit and push complete."