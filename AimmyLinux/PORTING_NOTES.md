# Linux Porting Notes

## Architecture Milestone Completed

The Linux port has been refactored into layered projects:

- `Aimmy.Core`
- `Aimmy.Platform.Abstractions`
- `Aimmy.Platform.Linux.X11`
- `Aimmy.Inference.OnnxRuntime`
- `Aimmy.UI.Avalonia`
- `Aimmy.Linux.App`

This commit establishes the migration baseline needed for phased feature parity.

## Implemented in This Commit

- Typed config model (`AimmyConfig`) with normalization and defaults.
- Dual-read config flow:
  - typed JSON
  - legacy `.cfg` migration path
- ONNX Runtime backend with runtime provider selection attempts:
  - CUDA
  - ROCm
  - CPU fallback
- Core targeting/prediction/movement modules:
  - Kalman
  - Shalloe
  - WiseTheFox
- Runtime diagnostics with FPS and p50/p95 latencies.
- Linux capability probe with explicit feature state reporting.
- Multi-monitor display offset + DPI-aware capture/overlay geometry.
- X11 display discovery service (`xrandr`) with selection wiring in runtime CLI.
- Avalonia configuration editor UI for display selection and data-collection settings.
- Linux backend factories:
  - capture (external tool fallback)
  - input (`uinput`/`ydotool`, `xdotool`, noop)
  - X11 hotkey backend with global grabs + poll fallback path
  - overlay noop backend
- `uinput` setup diagnostics with explicit remediation guidance for missing device/permissions.
- Runtime emergency-stop keybind handling with deterministic shutdown input release.
- Runtime model-switch keybind hot-swap path with backend/provider diagnostics and sticky-target reset.
- Sticky aim transition logic for reacquire/switch/drop behavior.
- Avalonia tabbed configuration editor expanded to cover input/runtime/aim/prediction/trigger/FOV/overlay/data sections.
- Capability badges are now surfaced in Avalonia, and unsupported sections are explicitly disabled with messages.
- Store and updater service scaffolds (GitHub API driven).
- Deb/RPM packaging script scaffolds.
- Parity and acceptance docs:
  - `docs/PARITY_MATRIX.md`
  - `docs/ACCEPTANCE_CHECKLIST.md`

## Still Pending for Full v1 Parity

- Full Avalonia parity for remaining Windows menus and advanced flows (model store/update views, capability badges, runtime dashboard).
- End-to-end validation against Ubuntu/Fedora hardware matrix.
