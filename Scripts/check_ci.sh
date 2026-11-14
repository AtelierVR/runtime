#!/usr/bin/env bash

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
GRAY='\033[0;90m'
NC='\033[0m'

# Default values
TARGET_DIR="."
FAILED_ONLY=false

# Parse command-line arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -f|--failed-only)
            FAILED_ONLY=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [path/to/repo] [-f|--failed-only]"
            echo "Options:"
            echo "  path/to/repo       Path to the target Git repository (default: .)"
            echo "  -f, --failed-only  Display only failed CI checks"
            exit 0
            ;;
        *)
            TARGET_DIR="$1"
            shift
            ;;
    esac
done

# Resolve absolute path and navigate to the target directory
if ! cd "$TARGET_DIR" 2>/dev/null; then
    echo -e "${RED}Error:${NC} Directory '$TARGET_DIR' not found."
    exit 1
fi

TARGET_DIR=$(pwd)

# Helper function to check CI status for any repository path
check_repo_ci() {
    local path="$1"
    local label="$2"
    local out_file="$3"

    # Get Repo Remote URL
    local repo_url
    if [ "$path" = "." ]; then
        repo_url=$(git config --get remote.origin.url 2>/dev/null)
    else
        repo_url=$(git config --file .gitmodules --get "submodule.${path}.url" 2>/dev/null)
    fi

    # Fetch run details via gh
    local json
    json=$(cd "$path" 2>/dev/null && gh run list --limit 1 --json databaseId,workflowName,status,conclusion,url 2>/dev/null)

    if [ -z "$json" ] || [ "$json" = "[]" ]; then
        if [ "$FAILED_ONLY" = false ]; then
            {
                echo -e "\n${BLUE}${label}:${NC} ${path}"
                echo -e "  ${GRAY}Repo:${NC} ${repo_url:-Unknown}"
                echo -e "  ${GRAY}CI Status:${NC} ${YELLOW}UNKNOWN / NO RUNS${NC}"
            } > "$out_file"
        fi
        return
    fi

    # Parse main JSON
    local run_id wf_name status conclusion run_url
    run_id=$(echo "$json" | jq -r '.[0].databaseId' 2>/dev/null)
    wf_name=$(echo "$json" | jq -r '.[0].workflowName // "Workflow"' 2>/dev/null)
    status=$(echo "$json" | jq -r '.[0].status' 2>/dev/null)
    conclusion=$(echo "$json" | jq -r '.[0].conclusion' 2>/dev/null)
    run_url=$(echo "$json" | jq -r '.[0].url' 2>/dev/null)

    # Filter out non-failing runs if --failed-only flag is set
    if [ "$FAILED_ONLY" = true ] && [ "$conclusion" != "failure" ]; then
        return
    fi

    local status_display
    local failed_step=""
    local failed_time=""

    if [ "$status" = "completed" ]; then
        if [ "$conclusion" = "success" ]; then
            status_display="${GREEN}SUCCESS${NC}"
        elif [ "$conclusion" = "failure" ]; then
            status_display="${RED}FAILED${NC}"

            # Fetch detailed view for failed step and timestamp
            local view_json
            view_json=$(cd "$path" 2>/dev/null && gh run view "$run_id" --json jobs 2>/dev/null)
            
            if [ -n "$view_json" ]; then
                # Extract first failing step name and completion time
                failed_step=$(echo "$view_json" | jq -r '.jobs[].steps[] | select(.conclusion=="failure") | .name' 2>/dev/null | head -n 1)
                failed_time=$(echo "$view_json" | jq -r '.jobs[].steps[] | select(.conclusion=="failure") | .completedAt' 2>/dev/null | head -n 1)
                
                # Format ISO date to readable string
                if [ -n "$failed_time" ] && [ "$failed_time" != "null" ]; then
                    failed_time=$(date -d "$failed_time" +"%Y-%m-%d %H:%M:%S UTC" 2>/dev/null || echo "$failed_time")
                fi
            fi
        else
            status_display="${YELLOW}${conclusion^^}${NC}"
        fi
    elif [ "$status" = "in_progress" ]; then
        status_display="${YELLOW}IN PROGRESS${NC}"
    else
        status_display="${YELLOW}${status^^}${NC}"
    fi

    {
        echo -e "\n${BLUE}${label}:${NC} ${path}"
        echo -e "  ${GRAY}Repo:${NC} ${repo_url:-Unknown}"
        echo -e "  ${GRAY}Workflow:${NC} ${wf_name:-Unknown}"
        echo -e "  ${GRAY}CI Status:${NC} $status_display"
        
        if [ "$conclusion" = "failure" ]; then
            [ -n "$failed_step" ] && [ "$failed_step" != "null" ] && echo -e "  ${GRAY}Failed Step:${NC} ${RED}${failed_step}${NC}"
            [ -n "$failed_time" ] && [ "$failed_time" != "null" ] && echo -e "  ${GRAY}Failed At:${NC} ${failed_time}"
            echo -e "  ${GRAY}Logs URL:${NC} ${run_url}"
        else
            echo -e "  ${GRAY}Run URL:${NC} ${run_url:-N/A}"
        fi
    } > "$out_file"
}

echo "Checking CI status for '$TARGET_DIR'..."

TMP_DIR=$(mktemp -d ./ci_check_tmp.XXXXXX)

# Read submodules into an array
mapfile -t submodules < <(git submodule status | awk '{print $2}')

pids=()

# 1. Start Root Repository check
check_repo_ci "." "Root Repository" "$TMP_DIR/root.txt" &
pids+=($!)

# 2. Start Submodules check
for i in "${!submodules[@]}"; do
    path="${submodules[$i]}"
    if [ -n "$path" ]; then
        check_repo_ci "$path" "Submodule" "$TMP_DIR/sub_$i.txt" &
        pids+=($!)
    fi
done

# Wait for all background jobs to finish
for pid in "${pids[@]}"; do
    wait "$pid" 2>/dev/null
done

# Print Root Repository result first
if [ -f "$TMP_DIR/root.txt" ]; then
    cat "$TMP_DIR/root.txt"
fi

# Print all Submodules results in order
for i in "${!submodules[@]}"; do
    if [ -f "$TMP_DIR/sub_$i.txt" ]; then
        cat "$TMP_DIR/sub_$i.txt"
    fi
done

# Clean temporary folder
rm -rf "$TMP_DIR"

echo -e "\nDone!"