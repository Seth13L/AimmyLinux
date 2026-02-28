#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APP_NAME="aimmylinux"
INSTALL_ROOT="/opt/aimmylinux"
PUBLISH_DIR="${PUBLISH_DIR:-$ROOT_DIR/artifacts/publish/linux-x64}"
DEB_BUILD_DIR="${DEB_BUILD_DIR:-$ROOT_DIR/artifacts/deb/${APP_NAME}_${VERSION}}"
DEB_OUTPUT="${DEB_BUILD_DIR}.deb"

rm -rf "$DEB_BUILD_DIR"
mkdir -p \
  "$DEB_BUILD_DIR/DEBIAN" \
  "$DEB_BUILD_DIR$INSTALL_ROOT" \
  "$DEB_BUILD_DIR/usr/bin" \
  "$DEB_BUILD_DIR/usr/share/applications" \
  "$DEB_BUILD_DIR/usr/share/doc/$APP_NAME"

cat > "$DEB_BUILD_DIR/DEBIAN/control" <<EOF
Package: ${APP_NAME}
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Aimmy Team
Depends: ca-certificates
Description: Aimmy Linux runtime (X11-first)
EOF

cat > "$DEB_BUILD_DIR/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
cat <<'MSG'
AimmyLinux post-install:
  1. Ensure runtime dependencies exist (python3, python3-tk, xdotool/ydotool).
  2. For uinput primary input path, ensure your user can access /dev/uinput.
  3. Start the UI with: aimmylinux --ui-config
MSG
exit 0
EOF
chmod 0755 "$DEB_BUILD_DIR/DEBIAN/postinst"

cat > "$DEB_BUILD_DIR/usr/bin/aimmylinux" <<'EOF'
#!/bin/sh
exec dotnet /opt/aimmylinux/AimmyLinux.dll "$@"
EOF
chmod 0755 "$DEB_BUILD_DIR/usr/bin/aimmylinux"

cat > "$DEB_BUILD_DIR/usr/share/applications/aimmylinux.desktop" <<'EOF'
[Desktop Entry]
Name=AimmyLinux
Comment=Aimmy Linux runtime
Exec=/usr/bin/aimmylinux --ui-config
Terminal=false
Type=Application
Categories=Utility;
StartupNotify=true
EOF
chmod 0644 "$DEB_BUILD_DIR/usr/share/applications/aimmylinux.desktop"

cat > "$DEB_BUILD_DIR/usr/share/doc/$APP_NAME/README.Debian" <<'EOF'
AimmyLinux package notes:
- Wayland active aim pipeline is unsupported in Linux v1.
- X11 session is required for active runtime behavior.
- Use `aimmylinux --check-update --current-version <version>` for package-aware update guidance.
EOF
chmod 0644 "$DEB_BUILD_DIR/usr/share/doc/$APP_NAME/README.Debian"

dotnet publish "$ROOT_DIR/AimmyLinux.csproj" -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"
cp -r "$PUBLISH_DIR"/* "$DEB_BUILD_DIR$INSTALL_ROOT/"

dpkg-deb --build "$DEB_BUILD_DIR" "$DEB_OUTPUT"
echo "Built DEB: $DEB_OUTPUT"
