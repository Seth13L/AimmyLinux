# Windows-to-Linux Functional Parity Ledger (X11 v1)

This ledger tracks Windows operational features against Linux implementation status.

## Runtime and Control Loop

| Windows Functional Item | Linux Equivalent | Status | Notes |
|---|---|---|---|
| Aim assist activation and hold semantics | `AimmyRuntime` + `IHotkeyBackend` | Implemented | Hold/constant tracking covered by runtime tests. |
| Sticky aim transitions | `StickyAimTracker` in `Aimmy.Core` | Implemented | Reacquire/switch/drop logic covered by tests. |
| Prediction methods (Kalman/Shalloe/WiseTheFox) | `PredictorFactory` + predictor classes | Implemented | Selection and output behavior tested. |
| EMA smoothing toggle/amount | `AimEmaSmoother` in runtime move path | Implemented | Config now actively affects movement deltas. |
| Trigger single/spray modes | `HandleTriggerAsync` + input backend | Implemented | Single click + hold/release spray behaviors. |
| Cursor check | Cursor-position box check | Implemented | Uses cursor provider, not crosshair fallback. |
| Dynamic FOV behavior | `DynamicFovResolver` + overlay updates | Implemented | Runtime keybind toggles dynamic FOV size. |
| Third-person masking | `ThirdPersonMaskApplier` | Implemented | Bottom-left mask applied before inference. |

## Capture/Input/Overlay

| Windows Functional Item | Linux Equivalent | Status | Notes |
|---|---|---|---|
| Screen capture methods | X11 native + external tool fallback | Implemented | Capture method selectable in UI. |
| Input movement methods | uinput/ydotool + xdotool fallback | Implemented | Factory fallback and diagnostics in place. |
| Global hotkeys | X11 grabs + polling fallback | Implemented | Degraded path explicitly reported. |
| FOV + ESP overlay rendering | X11 tkinter overlay backend | Implemented | Detection color/font/border now config-driven. |
| StreamGuard | Capability-declared unsupported | Excluded v1 | Explicitly surfaced as unavailable. |

## Configuration and UI

| Windows Functional Item | Linux Equivalent | Status | Notes |
|---|---|---|---|
| Legacy cfg compatibility | `LegacyCfgMigrator` | Implemented | Added mappings for model switch toggle, overlay style, UI/runtime toggles. |
| Typed config writes | `AimmyConfig` JSON save | Implemented | UI save path writes typed JSON only. |
| Live operational UI | Avalonia runtime shell tab + config tabs | Implemented (baseline) | Start/stop/apply runtime and live snapshot status included. |
| Model metadata-driven UX | `IModelMetadataReader` + model tab refresh | Implemented (baseline) | Dynamic/fixed-size and class metadata surfaced. |
| Store and updater controls | Store/Update tab + services | Implemented (baseline) | Update flow is check+guide (no in-app install). |

## Packaging and Release

| Windows Functional Item | Linux Equivalent | Status | Notes |
|---|---|---|---|
| Installable artifacts | Deb/RPM build scripts + CI artifact workflow | In progress | Install layout and desktop integration implemented; distro matrix verification still pending. |
| Update UX | Package-aware release selection + install guidance | In progress | Check+guide path now channel/package aware; package-manager verification matrix still pending. |
| Performance parity gate | Benchmark + diagnostics assertions | In progress | SHM capture optimization and matrix benchmarking pending. |
