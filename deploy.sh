#!/usr/bin/env sh
set -e
cd "$(dirname "$0")"
target="${CELESTE_DIR:?Set CELESTE_DIR to the Celeste folder}/Mods/ClimbTrials"

rsync -rc --inplace --delete --delete-excluded \
  --filter='P /bin/ClimbTrials.dll' --exclude='/bin/ClimbTrials.dll' \
  --include='/everest.yaml' --include='/bin/' --include='/ClimbTrials/' --include='/Dialog/' \
  --include='/Graphics/' --include='/Audio/' --include='/Ahorn/' --include='/Loenn/' \
  --exclude='/*' ./ "$target/"

rsync -c --inplace bin/ClimbTrials.dll "$target/bin/"
