# Linux Acceptance Checklist

## Functional Gates
- [x] Loads typed `aimmylinux.json`.
- [x] Reads legacy `.cfg` and migrates settings in memory.
- [x] Loads ONNX model and runs detections.
- [x] Performs aim movement with selected input backend.
- [x] Supports prediction strategy selection (`Kalman`, `Shalloe`, `WiseTheFox`).
- [x] Supports auto trigger single-click and spray behaviors.
- [x] Supports data collection image capture with optional auto-label output.
- [x] Supports model/config store listing and download operations.
- [x] Supports package-aware update check.
- [x] Supports integrated runtime shell controls (start/stop/apply + live snapshot feed).
- [x] Supports cursor-position trigger check when `Cursor Check` is enabled.

## Capability Gates
- [x] Capability flags print at startup and reflect environment.
- [x] Unsupported features are marked unavailable/degraded instead of failing silently.
- [x] Wayland reports explicit unsupported status for v1 aim pipeline.
- [x] StreamGuard is explicit as unsupported in Linux v1.

## Performance Gates
- [ ] Capture latency p50/p95 is logged.
- [ ] Inference latency p50/p95 is logged.
- [ ] Loop latency p50/p95 and effective FPS are logged.
- [ ] Linux baseline is within 15% of Windows baseline on matched hardware.

## Stability Gates
- [ ] 30-minute soak test on Ubuntu target matrix passes.
- [ ] 30-minute soak test on Fedora target matrix passes.
- [ ] No unhandled exceptions in runtime loop under normal operation.

## Packaging Gates
- [ ] Deb package build script produces installable artifact.
- [ ] RPM package build script produces installable artifact.
- [ ] Update guidance aligns with package channel behavior.

## Release Notes Gates
- [ ] Known limitations section includes Wayland status.
- [ ] Input permission setup (udev/groups) documented.
- [ ] Troubleshooting section covers missing backend tools.
