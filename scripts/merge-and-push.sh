#!/bin/bash

set -euo pipefail 

# --- Configuration ---
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
STARTING_PATH="$(pwd)" 
ORIGINAL_BRANCH=""     

# --- Functions ---
log_error() {
  echo "ERROR: $1" >&2
}

cleanup_and_exit() {
    local exit_code=$?
    echo "--- Operation Summary ---"
    if [ -n "${ORIGINAL_BRANCH}" ]; then
        echo "Attempting to switch back to original branch: ${ORIGINAL_BRANCH}"
        if git rev-parse --is-inside-work-tree &>/dev/null && [ "$(pwd)" == "${REPO_ROOT}" ]; then
            if ! git checkout "${ORIGINAL_BRANCH}"; then
                echo "Warning: Failed to switch back to original branch '${ORIGINAL_BRANCH}'. You might need to do it manually."
            else
                echo "Switched back to branch: ${ORIGINAL_BRANCH}"
            fi
        else
            echo "Warning: Not in a git repository or not in the project root. Cannot switch back to ${ORIGINAL_BRANCH} automatically."
        fi
    fi
    echo "Returning to original path: ${STARTING_PATH}"
    cd "${STARTING_PATH}" || echo "Warning: Could not return to starting path ${STARTING_PATH}"

    # if [ ${exit_code} -ne 0 ]; then
    #    echo "Script finished with errors."
    # fi
    exit ${exit_code}
}

trap cleanup_and_exit EXIT

# --- Argument Check ---
if [ "$#" -ne 2 ]; then
  log_error "Usage: $0 <feature-branch-to-merge> <target-branch>"
  log_error "Example: $0 my-feature main"
  exit 1
fi

FEATURE_BRANCH="$1"
TARGET_BRANCH="$2"

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


ORIGINAL_BRANCH=$(git symbolic-ref --short HEAD 2>/dev/null || git rev-parse --short HEAD 2>/dev/null)
if [ -z "${ORIGINAL_BRANCH}" ]; then
    log_error "Could not determine the current branch. Are you in a detached HEAD state?"
    exit 1
fi


echo "Current (original) branch: ${ORIGINAL_BRANCH}"
echo "Feature branch to merge: ${FEATURE_BRANCH}"
echo "Target branch: ${TARGET_BRANCH}"

if [ "${ORIGINAL_BRANCH}" != "${FEATURE_BRANCH}" ]; then
    echo "Warning: You are currently on branch '${ORIGINAL_BRANCH}', but specified '${FEATURE_BRANCH}' to be merged."
    echo "The script will proceed to merge '${FEATURE_BRANCH}' into '${TARGET_BRANCH}' and then attempt to switch back to '${ORIGINAL_BRANCH}'."
    read -p "Continue? (y/N): " confirm_branch
    if [[ ! "$confirm_branch" =~ ^[Yy]$ ]]; then
        echo "Operation cancelled by user."
        exit 0
    fi
fi


# --- Git Operations ---

echo "Fetching latest changes from remote..."
if ! git fetch --all --prune; then
    log_error "git fetch failed."
    exit 1
fi
echo "Fetch complete."

echo "Switching to target branch '${TARGET_BRANCH}'..."
if ! git checkout "${TARGET_BRANCH}"; then
  log_error "Failed to switch to target branch '${TARGET_BRANCH}'. Ensure it exists locally or remotely."
  exit 1
fi
echo "Switched to branch '${TARGET_BRANCH}'."

echo "Pulling latest changes for '${TARGET_BRANCH}'..."
if ! git pull origin "${TARGET_BRANCH}"; then
    log_error "Failed to pull latest changes for '${TARGET_BRANCH}'. Resolve conflicts if any and try again."
    exit 1
fi
echo "Latest changes pulled for '${TARGET_BRANCH}'."

echo "Merging feature branch '${FEATURE_BRANCH}' into '${TARGET_BRANCH}'..."
if ! git merge --no-ff "${FEATURE_BRANCH}"; then
  log_error "git merge failed. Please resolve conflicts manually, commit, and then run 'git push' for '${TARGET_BRANCH}'."
  exit 1
fi
echo "Branch '${FEATURE_BRANCH}' merged into '${TARGET_BRANCH}'."

echo "Pushing target branch '${TARGET_BRANCH}' to remote..."
if ! git push origin "${TARGET_BRANCH}"; then
  log_error "git push for '${TARGET_BRANCH}' failed."
  exit 1
fi
echo "Target branch '${TARGET_BRANCH}' pushed successfully."

echo "Merge and push complete."