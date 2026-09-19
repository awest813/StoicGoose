#!/usr/bin/env bash
# Cloud Agent start step for StoicGoose.
# Brings up a virtual framebuffer so the OpenGL GUI (StoicGoose.GLWindow) can run
# headlessly with Mesa software rendering. Idempotent across reboots.
set -euo pipefail

export DISPLAY="${DISPLAY:-:99}"

if xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
  echo "Xvfb already running on $DISPLAY"
  exit 0
fi

rm -f "/tmp/.X${DISPLAY#:}-lock"
Xvfb "$DISPLAY" -screen 0 1280x720x24 -ac +extension GLX +render -noreset >/tmp/xvfb.log 2>&1 &

for _ in $(seq 1 30); do
  if xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
    echo "Xvfb ready on $DISPLAY"
    exit 0
  fi
  sleep 0.5
done

echo "Xvfb failed to start on $DISPLAY" >&2
cat /tmp/xvfb.log >&2 || true
exit 1
