#!/usr/bin/env bash
set -euo pipefail

if [ -z "${TARGET_PLATFORM:-}" ]; then
  echo "::error::TARGET_PLATFORM is required"
  exit 1
fi

if [ -z "${ARTIFACT_NAME:-}" ]; then
  echo "::error::ARTIFACT_NAME is required"
  exit 1
fi

mkdir -p artifacts

build_dir="build/${TARGET_PLATFORM}"
if [ ! -d "$build_dir" ]; then
  echo "::error::Missing build output directory: $build_dir"
  exit 1
fi

(
  cd "$build_dir"
  zip -r "../../artifacts/${ARTIFACT_NAME}" .
)
