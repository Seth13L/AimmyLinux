using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using System.Runtime.InteropServices;

namespace Aimmy.Platform.Linux.X11.Hotkeys;

public sealed class X11HotkeyBackend : IHotkeyBackend
{
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

    private static readonly Dictionary<string, uint> MouseAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["left"] = Button1Mask,
        ["lbutton"] = Button1Mask,
        ["mouse1"] = Button1Mask,
        ["mb1"] = Button1Mask,
        ["middle"] = Button2Mask,
        ["mbutton"] = Button2Mask,
        ["mouse3"] = Button2Mask,
        ["mb3"] = Button2Mask,
        ["right"] = Button3Mask,
        ["rbutton"] = Button3Mask,
        ["mouse2"] = Button3Mask,
        ["mb2"] = Button3Mask,
        ["xbutton1"] = Button4Mask,
        ["mouse4"] = Button4Mask,
        ["mb4"] = Button4Mask,
        ["xbutton2"] = Button5Mask,
        ["mouse5"] = Button5Mask,
        ["mb5"] = Button5Mask
    };

    private readonly Func<string, string?> _environmentVariableReader;
    private readonly Dictionary<string, string> _configuredBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BindingProbe> _resolvedBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _bindingStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    private IntPtr _display;
    private IntPtr _rootWindow;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
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

    public string Name => "x11-hotkeys(global-poll)";

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

        return MouseAliasMap.TryGetValue(bindingName.Trim(), out buttonMask);
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
        if (TryResolveMouseButtonMask(configuredValue, out var mouseMask))
        {
            return BindingProbe.ForMouse(mouseMask);
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

    private enum BindingKind
    {
        None = 0,
        Keyboard = 1,
        MouseButton = 2
    }

    private readonly record struct BindingProbe(BindingKind Kind, byte KeyCode, uint MouseMask)
    {
        public static BindingProbe None => new(BindingKind.None, 0, 0);
        public static BindingProbe ForKey(byte keyCode) => new(BindingKind.Keyboard, keyCode, 0);
        public static BindingProbe ForMouse(uint mouseMask) => new(BindingKind.MouseButton, 0, mouseMask);
    }

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
}
