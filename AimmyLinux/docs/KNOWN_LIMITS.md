# Known Limits (Current Scaffold)

- Wayland active aim pipeline is unsupported by design for v1.
- Native X11 capture is implemented; SHM-specific optimization is still pending.
- Linux overlay backend uses a Python/tkinter renderer and may vary by distro desktop stack.
- Global hotkey backend requires X11 and `libX11`; fallback backend is used when unavailable.
- `uinput` path currently uses `ydotool` command integration.

These limits are surfaced through runtime capability flags.
