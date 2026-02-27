# Test Plan

## Unit
- Target selector returns nearest valid target.
- Prediction engines produce forward-biased aim points.
- Legacy cfg migration maps fields to typed config.

## Integration
- Capture backend returns non-empty frame.
- Capture fallback backend selection honors preferred/auto order and reports attempted backends on failure.
- Native X11 capture backend selection is validated with fallback to external tooling when unsupported.
- Input backend sends move/click commands without runtime exceptions.
- Input backend factory falls back from `uinput` to `xdotool`/`ydotool`/`noop` deterministically.
- Hotkey backend factory selects X11 global polling on supported systems and fallback hotkeys otherwise.
- Backend composition harness validates native-stack and fallback-stack selection/capability states under simulated X11 environments.
- Capability probe reports X11 vs non-X11 states without false-positive backend enablement.
- Runtime loop prints diagnostics and respects configured FPS.
- Runtime diagnostics assertions emit warnings when FPS or p95 latency thresholds are violated.
- Overlay backend factory selects X11 FOV renderer when prerequisites are available and falls back to noop otherwise.
- Overlay renderer projects capture-space detections into screen-space boxes/tracers with confidence text support.

## GPU Validation
- Auto mode selects CUDA on supported NVIDIA stack.
- Auto mode selects ROCm on supported AMD stack.
- CPU fallback activates with explicit reason when GPU EP unavailable.

## E2E
- Aim movement executes with selected backend.
- Auto trigger single and spray behavior operate as configured.
- Model store list/download flow succeeds against GitHub API.
- Update check returns package-aware release recommendation.

## Soak
- 30-minute run with stable FPS/latency metrics and no crash.
