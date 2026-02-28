#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APP_NAME="aimmylinux"
INSTALL_ROOT="/opt/aimmylinux"
RPMROOT="${RPMROOT:-$ROOT_DIR/artifacts/rpmroot}"
PUBLISH_DIR="${PUBLISH_DIR:-$ROOT_DIR/artifacts/publish/linux-x64}"
STAGE_DIR="${RPMROOT}/STAGE/${APP_NAME}-${VERSION}"
TARBALL="${RPMROOT}/SOURCES/${APP_NAME}-${VERSION}.tar.gz"
SPEC_FILE="${RPMROOT}/SPECS/${APP_NAME}.spec"

rm -rf "$RPMROOT"
mkdir -p "$RPMROOT"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS,STAGE}

dotnet publish "$ROOT_DIR/AimmyLinux.csproj" -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"

mkdir -p \
  "$STAGE_DIR$INSTALL_ROOT" \
  "$STAGE_DIR/usr/bin" \
  "$STAGE_DIR/usr/share/applications" \
  "$STAGE_DIR/usr/share/doc/$APP_NAME"
cp -r "$PUBLISH_DIR"/* "$STAGE_DIR$INSTALL_ROOT/"

cat > "$STAGE_DIR/usr/bin/aimmylinux" <<'EOF'
#!/bin/sh
exec dotnet /opt/aimmylinux/AimmyLinux.dll "$@"
EOF
chmod 0755 "$STAGE_DIR/usr/bin/aimmylinux"

cat > "$STAGE_DIR/usr/share/applications/aimmylinux.desktop" <<'EOF'
[Desktop Entry]
Name=AimmyLinux
Comment=Aimmy Linux runtime
Exec=/usr/bin/aimmylinux --ui-config
Terminal=false
Type=Application
Categories=Utility;
StartupNotify=true
EOF
chmod 0644 "$STAGE_DIR/usr/share/applications/aimmylinux.desktop"

cat > "$STAGE_DIR/usr/share/doc/$APP_NAME/README.RPM" <<'EOF'
AimmyLinux package notes:
- Wayland active aim pipeline is unsupported in Linux v1.
- X11 session is required for active runtime behavior.
- Use `aimmylinux --check-update --current-version <version>` for package-aware update guidance.
EOF
chmod 0644 "$STAGE_DIR/usr/share/doc/$APP_NAME/README.RPM"

tar -czf "$TARBALL" -C "$RPMROOT/STAGE" "${APP_NAME}-${VERSION}"

cat > "$SPEC_FILE" <<EOF
Name: ${APP_NAME}
Version: ${VERSION}
Release: 1%{?dist}
Summary: Aimmy Linux runtime (X11-first)
License: Source Available
URL: https://github.com/Seth13L/AimmyLinux
Source0: %{name}-%{version}.tar.gz
BuildArch: x86_64
Requires: ca-certificates

%description
Aimmy Linux runtime package.

%prep
%setup -q

%build
# no-op

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}
cp -a * %{buildroot}/

%post
echo "AimmyLinux installed. Run 'aimmylinux --ui-config' to open the UI."
echo "For uinput mode, ensure your user can access /dev/uinput."

%files
/opt/aimmylinux
/usr/bin/aimmylinux
/usr/share/applications/aimmylinux.desktop
/usr/share/doc/aimmylinux/README.RPM
EOF

rpmbuild --define "_topdir $RPMROOT" -bb "$SPEC_FILE"
echo "Built RPM under $RPMROOT/RPMS"
