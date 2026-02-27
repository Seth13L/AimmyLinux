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
- Linux backend factories:
  - capture (external tool fallback)
  - input (`uinput`/`ydotool`, `xdotool`, noop)
  - hotkey fallback backend
  - overlay noop backend
- Store and updater service scaffolds (GitHub API driven).
- Deb/RPM packaging script scaffolds.
- Parity and acceptance docs:
  - `docs/PARITY_MATRIX.md`
  - `docs/ACCEPTANCE_CHECKLIST.md`

## Still Pending for Full v1 Parity

- Display auto-discovery/selection UI wiring in Avalonia.
- Full Avalonia parity UI implementation.
- End-to-end validation against Ubuntu/Fedora hardware matrix.
