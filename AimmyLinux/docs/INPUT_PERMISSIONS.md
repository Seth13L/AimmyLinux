# Input Permissions (uinput / ydotool)

Aimmy Linux prefers a `uinput` path for low-latency input injection.

## Recommended setup

1. Install `ydotool`.
2. Ensure `/dev/uinput` exists.
3. Add user to the required input group for your distribution.
4. Re-login so group membership is refreshed.

## Fallbacks

If `uinput` setup is unavailable, Aimmy automatically falls back to:

- `xdotool`
- `ydotool` direct path
- noop backend (dry-run/validation)

Runtime capability logs show which input backend was selected.
