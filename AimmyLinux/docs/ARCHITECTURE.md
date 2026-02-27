# Architecture Overview

## Layering

- `Aimmy.Core`
  - Config model, enums, prediction engines, targeting, movement, diagnostics.
- `Aimmy.Platform.Abstractions`
  - Backend interfaces and transport models.
- `Aimmy.Platform.Linux.X11`
  - Linux capability probe, capture/input/hotkey/overlay backends, legacy cfg migrator.
- `Aimmy.Inference.OnnxRuntime`
  - ONNX Runtime inference implementation with runtime EP selection.
- `Aimmy.UI.Avalonia`
  - UI parity scaffold models/viewmodels.
- `AimmyLinux` (app host)
  - Composes runtime and service implementations.

## Runtime Flow

1. Load typed config (or migrate legacy cfg).
2. Probe capabilities.
3. Select backends (capture/input/hotkey/overlay/inference).
4. Capture frame -> detect -> select target -> predict -> move -> trigger.
5. Emit diagnostics and capability status.

## Feature-state Model

Each capability is reported with:

- `Enabled`
- `Disabled`
- `Unavailable`

with optional degraded reason text.
