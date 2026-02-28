# AimmyLinux (Layered Port Scaffold)

This folder now contains a layered Linux port architecture for Aimmy V2:

- `src/Aimmy.Core`: platform-agnostic logic (config, targeting, prediction, movement, diagnostics)
- `src/Aimmy.Platform.Abstractions`: backend interfaces and shared runtime models
- `src/Aimmy.Platform.Linux.X11`: Linux/X11 runtime backends and legacy `.cfg` migrator
- `src/Aimmy.Inference.OnnxRuntime`: ONNX Runtime backend with runtime GPU provider selection
- `src/Aimmy.UI.Avalonia`: Avalonia configuration editor UI
- `src/Aimmy.Linux.App`: app composition and runtime loop

Current implementation is an executable migration baseline for phased parity delivery.

## Dependencies

Install .NET SDK 8.0+ and runtime backends:

- Capture:
  - native X11 capture via `libX11` (primary)
  - `grim`
  - `maim`
  - `import` (ImageMagick)
  - `scrot`
- Input:
  - `ydotool` (preferred for `uinput`-centered path)
  - `xdotool` (fallback)
  - writable `/dev/uinput` (or `/dev/input/uinput`) for `uinput` primary path
- Overlay:
  - `python3` + `tkinter` (`python3-tk` package on Debian/Ubuntu)

Run dependency check:

```bash
cd AimmyLinux
./scripts/check-deps.sh
```

## Build

```bash
cd AimmyLinux
dotnet restore
dotnet build -c Release
```

## Linux X11 Smoke Tests

Run integration smoke tests under an X11 display:

```bash
cd AimmyLinux
export RUN_LINUX_X11_INTEGRATION=1
dotnet test tests/Aimmy.Core.Tests/Aimmy.Core.Tests.csproj -c Release --filter "Category=LinuxIntegration"
```

## Run

```bash
dotnet run -c Release -- --config aimmylinux.json
```

Useful flags:

```bash
dotnet run -c Release -- --model /path/to/model.onnx --fps 120 --dry-run true
dotnet run -c Release -- --list-model-store
dotnet run -c Release -- --check-update --current-version 3.0.0
dotnet run -c Release -- --list-displays
dotnet run -c Release -- --select-display DP-1 --save-config
dotnet run -c Release -- --use-primary-display --save-config
dotnet run -c Release -- --ui-config
dotnet run -c Release -- --save-config
```

## Config

- Primary config is typed JSON (`AimmyConfig`) in `aimmylinux.json`.
- Legacy Windows-style `.cfg` files are supported via migration at load time.
- Writes always use typed JSON.
- Multi-monitor and DPI settings are in `Capture`:
  - `DisplayOffsetX` / `DisplayOffsetY`: top-left origin of the target display in the X11 virtual desktop.
  - `DpiScaleX` / `DpiScaleY`: scaling factors applied to capture geometry, FOV sizing, and overlay projection.
- Data collection:
  - Set `DataCollection.CollectDataWhilePlaying=true` to save captured frames.
  - Set `DataCollection.AutoLabelData=true` to emit YOLO labels for selected detections.
  - Output paths are controlled by `DataCollection.ImagesDirectory` and `DataCollection.LabelsDirectory`.

## Capability Flags

Runtime prints capability states at startup (enabled/degraded/unavailable) for:

- session support (`X11Session`, `WaylandAimAssist`)
- capture/input backend availability
- hotkey/overlay readiness
- store/update services
- `uinput` setup diagnostics with actionable remediation hints
- runtime model hot-swap diagnostics (keybind-triggered reload attempts and provider state)

## Packaging

Package build scripts:

- Deb: `packaging/deb/build-deb.sh`
- RPM: `packaging/rpm/build-rpm.sh`

Both scripts now stage the app into `/opt/aimmylinux`, install `/usr/bin/aimmylinux` launcher, and add a desktop entry at `/usr/share/applications/aimmylinux.desktop`.

Run:

```bash
cd AimmyLinux
./packaging/deb/build-deb.sh 0.1.0
./packaging/rpm/build-rpm.sh 0.1.0
```

Updater behavior is `check+guide` only:

- `deb` guidance uses `sudo apt install ./<package>.deb`
- `rpm` guidance uses `sudo dnf install ./<package>.rpm`

See `docs/PACKAGING_GUIDE.md` for install/update notes and post-install permissions guidance.

## Limits in This Commit

- Native X11 capture is implemented; SHM-specific optimization is pending.
- Linux overlay uses a Python/tkinter renderer and may vary across desktop stacks.
- X11 global hotkeys use X11 key/button grabs with polling fallback semantics.
- Avalonia currently exposes a live runtime shell (start/stop/apply + snapshot status) and tabbed editing for model/display/input/runtime/aim/prediction/trigger/FOV/overlay/store/update/data. Full menu-by-menu parity is still pending.
- StreamGuard-equivalent behavior is explicitly unsupported on Linux v1.
