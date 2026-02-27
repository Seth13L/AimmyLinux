#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
RPMROOT="${RPMROOT:-$ROOT_DIR/artifacts/rpmroot}"
PUBLISH_DIR="${PUBLISH_DIR:-$ROOT_DIR/artifacts/publish}"
SPEC_FILE="$RPMROOT/SPECS/aimmylinux.spec"

rm -rf "$RPMROOT"
mkdir -p "$RPMROOT"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

dotnet publish "$ROOT_DIR/AimmyLinux.csproj" -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"
tar -czf "$RPMROOT/SOURCES/aimmylinux-${VERSION}.tar.gz" -C "$PUBLISH_DIR" .

cat > "$SPEC_FILE" <<EOF
Name: aimmylinux
Version: ${VERSION}
Release: 1%{?dist}
Summary: Aimmy Linux runtime (X11-first)
License: Source Available
Source0: %{name}-%{version}.tar.gz
BuildArch: x86_64

%description
Aimmy Linux runtime package.

%prep
mkdir -p %{_builddir}/aimmylinux
cd %{_builddir}/aimmylinux
tar -xzf %{SOURCE0}

%install
mkdir -p %{buildroot}/usr/local/bin/aimmylinux
cp -r %{_builddir}/aimmylinux/* %{buildroot}/usr/local/bin/aimmylinux/

%files
/usr/local/bin/aimmylinux
EOF

rpmbuild --define "_topdir $RPMROOT" -bb "$SPEC_FILE"
echo "Built RPM under $RPMROOT/RPMS"
