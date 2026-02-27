using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using System.Runtime.InteropServices;

namespace Aimmy.Platform.Linux.X11.Hotkeys;

public sealed class X11HotkeyBackend : IHotkeyBackend
{
    private const uint LockMask = 1u << 1;
    private const uint Mod2Mask = 1u << 4;
    private const long ButtonPressMask = 1L << 2;
    private const long ButtonReleaseMask = 1L << 3;
    private const int GrabModeAsync = 1;

    private const uint Button1Mask = 1u << 8;
    private const uint Button2Mask = 1u << 9;
    private const uint Button3Mask = 1u << 10;
    private const uint Button4Mask = 1u << 11;
    private const uint Button5Mask = 1u << 12;

    private static readonly Dictionary<string, string> KeyAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["leftalt"] = "Alt_L",
        ["rightalt"] = "Alt_R",
        ["leftctrl"] = "Control_L",
        ["rightctrl"] = "Control_R",
        ["leftshift"] = "Shift_L",
        ["rightshift"] = "Shift_R",
        ["leftwin"] = "Super_L",
        ["rightwin"] = "Super_R",
        ["enter"] = "Return",
        ["esc"] = "Escape",
        ["escape"] = "Escape",
        ["space"] = "space",
        ["tab"] = "Tab",
        ["capslock"] = "Caps_Lock",
        ["numlock"] = "Num_Lock",
        ["scrolllock"] = "Scroll_Lock",
        ["prtsc"] = "Print",
        ["printscreen"] = "Print",
        ["pause"] = "Pause",
        ["insert"] = "Insert",
        ["backspace"] = "BackSpace",
        ["pageup"] = "Page_Up",
        ["pagedown"] = "Page_Down",
        ["home"] = "Home",
        ["end"] = "End",
        ["uparrow"] = "Up",
        ["downarrow"] = "Down",
        ["leftarrow"] = "Left",
        ["rightarrow"] = "Right"
    };

    private static readonly Dictionary<string, MouseBinding> MouseAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["left"] = new MouseBinding(Button1Mask, 1),
        ["lbutton"] = new MouseBinding(Button1Mask, 1),
        ["mouse1"] = new MouseBinding(Button1Mask, 1),
        ["mb1"] = new MouseBinding(Button1Mask, 1),
        ["middle"] = new MouseBinding(Button2Mask, 2),
        ["mbutton"] = new MouseBinding(Button2Mask, 2),
        ["mouse3"] = new MouseBinding(Button2Mask, 2),
        ["mb3"] = new MouseBinding(Button2Mask, 2),
        ["right"] = new MouseBinding(Button3Mask, 3),
        ["rbutton"] = new MouseBinding(Button3Mask, 3),
        ["mouse2"] = new MouseBinding(Button3Mask, 3),
        ["mb2"] = new MouseBinding(Button3Mask, 3),
        ["xbutton1"] = new MouseBinding(Button4Mask, 4),
        ["mouse4"] = new MouseBinding(Button4Mask, 4),
        ["mb4"] = new MouseBinding(Button4Mask, 4),
        ["xbutton2"] = new MouseBinding(Button5Mask, 5),
        ["mouse5"] = new MouseBinding(Button5Mask, 5),
        ["mb5"] = new MouseBinding(Button5Mask, 5)
    };

    private readonly Func<string, string?> _environmentVariableReader;
    private readonly Dictionary<string, string> _configuredBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BindingProbe> _resolvedBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _bindingStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<GrabbedKey> _grabbedKeys = new();
    private readonly HashSet<GrabbedButton> _grabbedButtons = new();
    private readonly object _sync = new();

    private IntPtr _display;
    private IntPtr _rootWindow;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private string _backendName = "x11-hotkeys(global-poll)";
    private bool _started;
    private bool _disposed;

    public X11HotkeyBackend(
        AimmyConfig config,
        Func<string, string?>? environmentVariableReader = null)
    {
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;

        _configuredBindings["Aim Keybind"] = config.Input.AimKeybind;
        _configuredBindings["Second Aim Keybind"] = config.Input.SecondaryAimKeybind;
        _configuredBindings["Dynamic FOV Keybind"] = config.Input.DynamicFovKeybind;
        _configuredBindings["Emergency Stop Keybind"] = config.Input.EmergencyStopKeybind;
        _configuredBindings["Model Switch Keybind"] = config.Input.ModelSwitchKeybind;
    }

    public string Name
    {
        get
        {
            lock (_sync)
            {
                return _backendName;
            }
        }
    }

    public static bool IsSupported(
        Func<string, string?> environmentVariableReader,
        out string reason)
    {
        if (!OperatingSystem.IsLinux())
        {
            reason = "X11 hotkey backend is only supported on Linux.";
            return false;
        }

        var sessionType = environmentVariableReader("XDG_SESSION_TYPE") ?? string.Empty;
        var display = environmentVariableReader("DISPLAY");
        var isX11 = string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(display);

        if (!isX11)
        {
            reason = "X11 session not detected.";
            return false;
        }

        try
        {
            var probeDisplay = XOpenDisplay(IntPtr.Zero);
            if (probeDisplay == IntPtr.Zero)
            {
                reason = "Unable to open X11 display.";
                return false;
            }

            XCloseDisplay(probeDisplay);
            reason = "X11 global key polling is supported.";
            return true;
        }
        catch (DllNotFoundException)
        {
            reason = "libX11 is not available.";
            return false;
        }
        catch (Exception ex)
        {
            reason = $"X11 probe failed: {ex.Message}";
            return false;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(X11HotkeyBackend));
            }

            if (_started)
            {
                return Task.CompletedTask;
            }

            if (!IsSupported(_environmentVariableReader, out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to open X11 display.");
            }

            _rootWindow = XDefaultRootWindow(_display);
            ResolveBindingsLocked();

            if (TryRegisterGlobalGrabsLocked(out var grabReason))
            {
                _backendName = "x11-hotkeys(grab+poll)";
            }
            else
            {
                _backendName = "x11-hotkeys(global-poll)";
                if (!string.IsNullOrWhiteSpace(grabReason))
                {
                    Console.WriteLine($"Hotkey grab fallback active: {grabReason}");
                }
            }

            _pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pollTask = Task.Run(() => PollAsync(_pollCts.Token), CancellationToken.None);
            _started = true;
        }

        return Task.CompletedTask;
    }

    public bool IsPressed(string bindingId)
    {
        lock (_sync)
        {
            return _bindingStates.TryGetValue(bindingId, out var pressed) && pressed;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task? pollTask;
        CancellationTokenSource? pollCts;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pollTask = _pollTask;
            pollCts = _pollCts;
            _pollTask = null;
            _pollCts = null;
        }

        try
        {
            pollCts?.Cancel();
            if (pollTask is not null)
            {
                await pollTask.ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore dispose-time poll cancellation errors.
        }
        finally
        {
            pollCts?.Dispose();
            ReleaseGlobalGrabsUnsafe();
            CloseDisplayUnsafe();
        }
    }

    public static bool TryResolveMouseButtonMask(string bindingName, out uint buttonMask)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
        {
            buttonMask = 0;
            return false;
        }

        if (!MouseAliasMap.TryGetValue(bindingName.Trim(), out var mouseBinding))
        {
            buttonMask = 0;
            return false;
        }

        buttonMask = mouseBinding.Mask;
        return true;
    }

    public static string CanonicalizeKeysymName(string bindingName)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
        {
            return string.Empty;
        }

        var trimmed = bindingName.Trim();
        return KeyAliasMap.TryGetValue(trimmed, out var alias)
            ? alias
            : trimmed;
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var keymap = new byte[32];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                XQueryKeymap(_display, keymap);
                _ = XQueryPointer(
                    _display,
                    _rootWindow,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var pointerMask);

                lock (_sync)
                {
                    foreach (var (bindingId, probe) in _resolvedBindings)
                    {
                        _bindingStates[bindingId] = probe.Kind switch
                        {
                            BindingKind.Keyboard => IsKeyCodePressed(keymap, probe.KeyCode),
                            BindingKind.MouseButton => (pointerMask & probe.MouseMask) != 0,
                            _ => false
                        };
                    }
                }
            }
            catch
            {
                // Any polling error deactivates all bindings but keeps runtime alive.
                lock (_sync)
                {
                    foreach (var key in _bindingStates.Keys.ToArray())
                    {
                        _bindingStates[key] = false;
                    }
                }
                break;
            }

            try
            {
                await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void ResolveBindingsLocked()
    {
        _resolvedBindings.Clear();
        _bindingStates.Clear();

        foreach (var (bindingId, configuredValue) in _configuredBindings)
        {
            var probe = ResolveBinding(configuredValue);
            _resolvedBindings[bindingId] = probe;
            _bindingStates[bindingId] = false;

            if (probe.Kind == BindingKind.None)
            {
                Console.WriteLine($"Hotkey warning: could not resolve binding '{bindingId}' -> '{configuredValue}'.");
            }
        }
    }

    private BindingProbe ResolveBinding(string configuredValue)
    {
        if (TryResolveMouseButton(configuredValue, out var mouseMask, out var mouseButton))
        {
            return BindingProbe.ForMouse(mouseMask, mouseButton);
        }

        var keysymName = CanonicalizeKeysymName(configuredValue);
        if (string.IsNullOrWhiteSpace(keysymName))
        {
            return BindingProbe.None;
        }

        var keysym = XStringToKeysym(keysymName);
        if (keysym == 0)
        {
            return BindingProbe.None;
        }

        var keyCode = XKeysymToKeycode(_display, keysym);
        return keyCode == 0
            ? BindingProbe.None
            : BindingProbe.ForKey(keyCode);
    }

    private bool TryRegisterGlobalGrabsLocked(out string reason)
    {
        _grabbedKeys.Clear();
        _grabbedButtons.Clear();

        try
        {
            if (_display == IntPtr.Zero || _rootWindow == IntPtr.Zero)
            {
                reason = "Display is not initialized for global grabs.";
                return false;
            }

            var keyboardCodes = _resolvedBindings.Values
                .Where(binding => binding.Kind == BindingKind.Keyboard && binding.KeyCode != 0)
                .Select(binding => (int)binding.KeyCode)
                .Distinct()
                .ToArray();

            var mouseButtons = _resolvedBindings.Values
                .Where(binding => binding.Kind == BindingKind.MouseButton && binding.MouseButton != 0)
                .Select(binding => binding.MouseButton)
                .Distinct()
                .ToArray();

            if (keyboardCodes.Length == 0 && mouseButtons.Length == 0)
            {
                reason = "No resolvable hotkey bindings were found for X11 grabs.";
                return false;
            }

            var modifierVariants = new[] { 0u, LockMask, Mod2Mask, LockMask | Mod2Mask };

            foreach (var keyCode in keyboardCodes)
            {
                foreach (var modifiers in modifierVariants)
                {
                    XGrabKey(_display, keyCode, modifiers, _rootWindow, true, GrabModeAsync, GrabModeAsync);
                    _grabbedKeys.Add(new GrabbedKey(keyCode, modifiers));
                }
            }

            foreach (var button in mouseButtons)
            {
                foreach (var modifiers in modifierVariants)
                {
                    XGrabButton(
                        _display,
                        button,
                        modifiers,
                        _rootWindow,
                        true,
                        (uint)(ButtonPressMask | ButtonReleaseMask),
                        GrabModeAsync,
                        GrabModeAsync,
                        IntPtr.Zero,
                        IntPtr.Zero);
                    _grabbedButtons.Add(new GrabbedButton(button, modifiers));
                }
            }

            _ = XSync(_display, false);
            reason = "X11 global key/button grabs registered.";
            return true;
        }
        catch (Exception ex)
        {
            reason = $"Failed to register X11 grabs: {ex.Message}";
            ReleaseGlobalGrabsUnsafe();
            return false;
        }
    }

    private static bool TryResolveMouseButton(string bindingName, out uint buttonMask, out uint button)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
        {
            buttonMask = 0;
            button = 0;
            return false;
        }

        if (!MouseAliasMap.TryGetValue(bindingName.Trim(), out var mouseBinding))
        {
            buttonMask = 0;
            button = 0;
            return false;
        }

        buttonMask = mouseBinding.Mask;
        button = mouseBinding.Button;
        return true;
    }

    private static bool IsKeyCodePressed(byte[] keymap, byte keyCode)
    {
        if (keyCode == 0)
        {
            return false;
        }

        var index = keyCode / 8;
        var mask = 1 << (keyCode % 8);
        return index < keymap.Length && (keymap[index] & mask) != 0;
    }

    private void CloseDisplayUnsafe()
    {
        lock (_sync)
        {
            if (_display != IntPtr.Zero)
            {
                try
                {
                    XCloseDisplay(_display);
                }
                catch
                {
                    // Ignore display close errors.
                }
                finally
                {
                    _display = IntPtr.Zero;
                    _rootWindow = IntPtr.Zero;
                }
            }
        }
    }

    private void ReleaseGlobalGrabsUnsafe()
    {
        lock (_sync)
        {
            if (_display == IntPtr.Zero || _rootWindow == IntPtr.Zero)
            {
                _grabbedKeys.Clear();
                _grabbedButtons.Clear();
                return;
            }

            foreach (var key in _grabbedKeys)
            {
                try
                {
                    XUngrabKey(_display, key.KeyCode, key.Modifiers, _rootWindow);
                }
                catch
                {
                    // Ignore ungrab failures during shutdown.
                }
            }

            foreach (var button in _grabbedButtons)
            {
                try
                {
                    XUngrabButton(_display, button.Button, button.Modifiers, _rootWindow);
                }
                catch
                {
                    // Ignore ungrab failures during shutdown.
                }
            }

            _grabbedKeys.Clear();
            _grabbedButtons.Clear();

            try
            {
                _ = XSync(_display, false);
            }
            catch
            {
                // Ignore sync failures during shutdown.
            }
        }
    }

    private enum BindingKind
    {
        None = 0,
        Keyboard = 1,
        MouseButton = 2
    }

    private readonly record struct BindingProbe(BindingKind Kind, byte KeyCode, uint MouseMask, uint MouseButton)
    {
        public static BindingProbe None => new(BindingKind.None, 0, 0, 0);
        public static BindingProbe ForKey(byte keyCode) => new(BindingKind.Keyboard, keyCode, 0, 0);
        public static BindingProbe ForMouse(uint mouseMask, uint mouseButton) => new(BindingKind.MouseButton, 0, mouseMask, mouseButton);
    }

    private readonly record struct MouseBinding(uint Mask, uint Button);
    private readonly record struct GrabbedKey(int KeyCode, uint Modifiers);
    private readonly record struct GrabbedButton(uint Button, uint Modifiers);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XQueryKeymap(IntPtr display, [Out] byte[] keysReturn);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr rootReturn,
        out IntPtr childReturn,
        out int rootXReturn,
        out int rootYReturn,
        out int winXReturn,
        out int winYReturn,
        out uint maskReturn);

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    private static extern nuint XStringToKeysym(string @string);

    [DllImport("libX11.so.6")]
    private static extern byte XKeysymToKeycode(IntPtr display, nuint keysym);

    [DllImport("libX11.so.6")]
    private static extern void XGrabKey(
        IntPtr display,
        int keycode,
        uint modifiers,
        IntPtr grabWindow,
        bool ownerEvents,
        int pointerMode,
        int keyboardMode);

    [DllImport("libX11.so.6")]
    private static extern void XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [DllImport("libX11.so.6")]
    private static extern void XGrabButton(
        IntPtr display,
        uint button,
        uint modifiers,
        IntPtr grabWindow,
        bool ownerEvents,
        uint eventMask,
        int pointerMode,
        int keyboardMode,
        IntPtr confineTo,
        IntPtr cursor);

    [DllImport("libX11.so.6")]
    private static extern void XUngrabButton(IntPtr display, uint button, uint modifiers, IntPtr grabWindow);

    [DllImport("libX11.so.6")]
    private static extern int XSync(IntPtr display, bool discard);
}
