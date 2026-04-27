#!/usr/bin/env bash
set -euo pipefail

missing=()
[ -z "${UNITY_LICENSE:-}" ]  && missing+=(UNITY_LICENSE)
[ -z "${UNITY_EMAIL:-}" ]    && missing+=(UNITY_EMAIL)
[ -z "${UNITY_PASSWORD:-}" ] && missing+=(UNITY_PASSWORD)

if [ ${#missing[@]} -gt 0 ]; then
    echo "::error::Missing required secrets: ${missing[*]}"
    exit 1
fi

echo "All required secrets are present."
