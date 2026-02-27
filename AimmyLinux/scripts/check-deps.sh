#!/usr/bin/env bash
set -euo pipefail

echo "Checking Linux runtime dependencies..."

check_cmd() {
  local name="$1"
  if command -v "$name" >/dev/null 2>&1; then
    echo "  [ok] $name"
  else
    echo "  [missing] $name"
  fi
}

check_cmd dotnet
check_cmd curl
check_cmd jq
check_cmd python3
check_cmd xdotool
check_cmd ydotool
check_cmd grim
check_cmd maim
check_cmd import
check_cmd scrot

echo
echo "At least one input backend and one capture backend are required."
echo "For uinput path, ensure /dev/uinput exists and user has required group permissions."
