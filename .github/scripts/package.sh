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

# game-ci sets -customBuildPath to build/{platform}/{buildName} (buildName defaults to
# targetPlatform), and Builder.Build appends BuildName one more time as the filename root.
# The actual output directory is therefore build/{platform}/{platform}/.
build_dir="build/${TARGET_PLATFORM}/${TARGET_PLATFORM}"
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
  zip -r "../../../artifacts/${ARTIFACT_NAME}" . -x "*.DS_Store" -x "__MACOSX/*"
)
