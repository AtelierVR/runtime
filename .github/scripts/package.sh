#!/usr/bin/env bash
set -euo pipefail

if [ -z "${ARTIFACT_NAME:-}" ]; then
  echo "::error::ARTIFACT_NAME is required"
  exit 1
fi

mkdir -p artifacts

build_dir="build"
if [ ! -d "$build_dir" ]; then
  echo "::error::Missing build output directory: $build_dir"
  exit 1
fi

echo "=== Build directory contents ==="
find "$build_dir" -maxdepth 3 | head -50

file_count=$(find "$build_dir" -type f | wc -l)
if [ "$file_count" -eq 0 ]; then
  echo "::error::Build directory '$build_dir' contains no files. Unity build may have failed."
  exit 1
fi

echo "Found $file_count file(s), packaging..."

(
  cd "$build_dir"
  zip -r "../artifacts/${ARTIFACT_NAME}" . -x "*.DS_Store" -x "__MACOSX/*"
)
