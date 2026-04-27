#!/usr/bin/env bash
set -euo pipefail

if [ -z "${DIST_DIR:-}" ]; then
  DIST_DIR="dist"
fi

cd "$DIST_DIR"
shopt -s nullglob
files=( *.zip )
if [ ${#files[@]} -eq 0 ]; then
  echo "::error::No zip artifacts found in ${DIST_DIR}/ to hash"
  exit 1
fi

sha256sum "${files[@]}" > SHA256SUMS.txt
