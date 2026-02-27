#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PUBLISH_DIR="${PUBLISH_DIR:-$ROOT_DIR/artifacts/publish}"
DEB_DIR="${DEB_DIR:-$ROOT_DIR/artifacts/deb/aimmylinux_${VERSION}}"

rm -rf "$DEB_DIR"
mkdir -p "$DEB_DIR/DEBIAN" "$DEB_DIR/usr/local/bin/aimmylinux"

cat > "$DEB_DIR/DEBIAN/control" <<EOF
Package: aimmylinux
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Aimmy Team
Description: Aimmy Linux runtime (X11-first)
EOF

dotnet publish "$ROOT_DIR/AimmyLinux.csproj" -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"
cp -r "$PUBLISH_DIR"/* "$DEB_DIR/usr/local/bin/aimmylinux/"

dpkg-deb --build "$DEB_DIR"
echo "Built DEB: ${DEB_DIR}.deb"
