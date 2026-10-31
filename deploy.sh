#!/usr/bin/env sh
set -e
cd "$(dirname "$0")"
target="${CELESTE_DIR:?Set CELESTE_DIR to the Celeste folder}/Mods/ClimbTrials"

changed_files=$(rsync -rc --inplace --delete --delete-excluded --itemize-changes \
  --filter='P /bin/ClimbTrials.dll' --exclude='/bin/ClimbTrials.dll' \
  --include='/everest.yaml' --include='/bin/' --include='/ClimbTrials/' --include='/Dialog/' \
  --include='/Graphics/' --include='/Audio/' --include='/Ahorn/' --include='/Loenn/' \
  --exclude='/*' ./ "$target/")

changed_code=$(rsync -c --inplace --itemize-changes bin/ClimbTrials.dll "$target/bin/")

if [ -z "$changed_code" ] && printf '%s\n' "$changed_files" | grep -q ' ClimbTrials/'; then
  touch "$target/bin/ClimbTrials.dll"
fi
