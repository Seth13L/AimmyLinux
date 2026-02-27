# Linux Parity Matrix (X11 v1)

## Scope
- Status definitions:
  - `Done`: implemented in Linux runtime scaffold.
  - `In Progress`: interface and baseline behavior available.
  - `Planned`: not yet implemented in this commit.
  - `Excluded v1`: explicitly out of scope for Linux v1.

## Core Runtime
| Area | Windows | Linux v1 | Status |
|---|---|---|---|
| ONNX inference loop | Yes | Yes | Done |
| Typed config model | No (dynamic dictionaries) | Yes | Done |
| Legacy `.cfg` read | Yes | Yes (migrated to typed model) | Done |
| JSON write config | Partial | Yes | Done |
| Runtime capability flags | No | Yes | Done |
| Runtime diagnostics (fps/latency) | Partial benchmark logs | Yes (p50/p95 snapshots) | Done |

## Capture and Input
| Area | Windows | Linux v1 | Status |
|---|---|---|---|
| DirectX desktop duplication | Yes | No | Excluded v1 |
| GDI capture fallback | Yes | No | Excluded v1 |
| X11 native capture backend | N/A | Yes | Done |
| External capture backends (grim/maim/import/scrot) | N/A | Yes | Done |
| Display selection + DPI scaling | Yes | Yes (display offsets + scaling factors) | Done |
| Display auto-discovery | Yes | Yes (`xrandr` discovery + selection) | Done |
| uinput-first input policy | N/A | Yes (uinput via ydotool path) | In Progress |
| xdotool fallback | N/A | Yes | Done |
| ydotool fallback | N/A | Yes | Done |

## Aim / Prediction / Trigger
| Area | Windows | Linux v1 | Status |
|---|---|---|---|
| Closest target selection | Yes | Yes | Done |
| Sticky aim | Yes | Baseline | In Progress |
| Kalman prediction | Yes | Yes | Done |
| Shalloe prediction | Yes | Yes | Done |
| WiseTheFox prediction | Yes | Yes | Done |
| Movement path variants | Yes | Yes | Done |
| Auto trigger (single click) | Yes | Yes | Done |
| Spray mode | Yes | Yes | Done |
| Cursor check | Yes | Crosshair-in-box trigger gating | Done |
| Data collection + auto-label | Yes | Yes | Done |

## Overlay / UI / Hotkeys
| Area | Windows | Linux v1 | Status |
|---|---|---|---|
| FOV overlay | Yes | Yes (X11 tkinter overlay) | Done |
| ESP overlay | Yes | Yes (boxes/confidence/tracers) | Done |
| Global hotkey capture UX | Yes | Fallback backend | In Progress |
| Full desktop GUI | WPF | Avalonia config editor window + viewmodel wiring | In Progress |

## Store / Update / Packaging
| Area | Windows | Linux v1 | Status |
|---|---|---|---|
| Model/config store | Yes | GitHub client + listing/download API | In Progress |
| In-app updater | Yes | Package-aware update check service | In Progress |
| Deb packaging | No | Packaging scaffold | In Progress |
| RPM packaging | No | Packaging scaffold | In Progress |

## Explicit Exclusions for v1
- Wayland active aim pipeline.
- Windows vendor-specific input drivers (LG Hub, Razer, ddxoft).
- DirectX/GDI capture methods.
