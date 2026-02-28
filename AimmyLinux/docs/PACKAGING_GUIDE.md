# Packaging Guide (Deb/RPM)

## Build Artifacts

From `AimmyLinux/`:

```bash
./packaging/deb/build-deb.sh 0.1.0
./packaging/rpm/build-rpm.sh 0.1.0
```

Outputs:

- Deb: `artifacts/deb/aimmylinux_<version>.deb`
- RPM: `artifacts/rpmroot/RPMS/**/aimmylinux-<version>-1*.rpm`

## Install Layout

Both package types install:

- App files: `/opt/aimmylinux`
- CLI launcher: `/usr/bin/aimmylinux`
- Desktop entry: `/usr/share/applications/aimmylinux.desktop`

## Update Behavior

Linux v1 updater is `check+guide` only (no privileged in-app install flow).

- Deb channel: install with `sudo apt install ./<package>.deb`
- RPM channel: install with `sudo dnf install ./<package>.rpm`

Run update check:

```bash
aimmylinux --check-update --current-version 3.0.0
```

## Post-Install Setup

Recommended dependency checks:

1. Run `./scripts/check-deps.sh`.
2. Ensure X11 session is active for runtime pipeline.
3. Ensure `ydotool` or `xdotool` is installed.
4. Ensure your runtime user can access `/dev/uinput` for primary input path.

If `uinput` is unavailable, fallback input methods will be selected and surfaced in runtime capability diagnostics.
