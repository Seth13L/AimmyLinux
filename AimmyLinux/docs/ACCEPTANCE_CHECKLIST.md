# Linux Acceptance Checklist

## Functional Gates
- [ ] Loads typed `aimmylinux.json`.
- [ ] Reads legacy `.cfg` and migrates settings in memory.
- [ ] Loads ONNX model and runs detections.
- [ ] Performs aim movement with selected input backend.
- [ ] Supports prediction strategy selection (`Kalman`, `Shalloe`, `WiseTheFox`).
- [ ] Supports auto trigger single-click and spray behaviors.
- [ ] Supports model/config store listing and download operations.
- [ ] Supports package-aware update check.

## Capability Gates
- [ ] Capability flags print at startup and reflect environment.
- [ ] Unsupported features are marked unavailable/degraded instead of failing silently.
- [ ] Wayland reports explicit unsupported status for v1 aim pipeline.

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
