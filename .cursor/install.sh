#!/usr/bin/env bash
# Cloud Agent install step for StoicGoose.
# Idempotent: safe to run repeatedly and against a cached/snapshot filesystem.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# --- .NET 9 SDK (pinned by global.json; not in the Ubuntu 24.04 feed) ---
if ! dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  sudo bash /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/lib/dotnet
  sudo ln -sf /usr/lib/dotnet/dotnet /usr/local/bin/dotnet
fi

# --- System libraries for the headless OpenGL/GLFW GUI and OpenAL audio ---
sudo apt-get update
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
  xvfb x11-utils \
  libgl1-mesa-dri libglx-mesa0 libgl1 libglu1-mesa \
  libx11-6 libxrandr2 libxi6 libxcursor1 libxinerama1 libxext6 libxfixes3 libxrender1 \
  libopenal1

# --- Default headless-display env for the OpenGL GUI (StoicGoose.GLWindow) ---
sudo tee /etc/profile.d/stoicgoose-dev.sh >/dev/null <<'EOF'
export DISPLAY="${DISPLAY:-:99}"
export LIBGL_ALWAYS_SOFTWARE=1
export GALLIUM_DRIVER=llvmpipe
EOF

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# --- Restore and build the solution ---
dotnet restore StoicGoose.sln
dotnet build StoicGoose.sln -c Release --no-restore
