#!/usr/bin/env sh
LOG="${CELESTE_DIR:?Set CELESTE_DIR to the Celeste folder}/log.txt"
FILTER="${1:-\[ClimbTrials/|\[Error\]}"

get_log_size() {
  stat -c %s "$LOG" 2>/dev/null || echo 0
}

tail_log() {
  offset=$(get_log_size)
  while true; do
    size=$(get_log_size)
    if [ "$size" -lt "$offset" ]; then
      offset=0
    fi
    if [ "$size" -gt "$offset" ]; then
      tail -c +$((offset + 1)) "$LOG" | head -c $((size - offset))
      offset=$size
    fi
    sleep 0.25
  done
}

format() {
  sed -u -E \
    -e 's/\r$//' \
    -e 's/^\([^ ]* ([0-9:]*)\) \[Everest\] /\1 /' \
    -e 's/ \[Info\] / /' \
    -e 's/\[ClimbTrials\//[CT\//'
}

echo "Tailing $LOG"
echo "Filter: $FILTER"
echo "---"
tail_log | grep --line-buffered -E "$FILTER" | format
