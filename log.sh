#!/usr/bin/env sh
cd "$(dirname "$0")"
LOG="$(cd ../.. && pwd)/log.txt"
PATTERN="${1:-ClimbTrials/}"
echo "Tailing $LOG"
echo "Filter: \"$PATTERN\""
echo "---"
tail -n 0 -F -s 0.25 "$LOG" | grep --line-buffered "$PATTERN"