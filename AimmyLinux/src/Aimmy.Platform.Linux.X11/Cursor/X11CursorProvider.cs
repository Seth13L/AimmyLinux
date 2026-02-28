using Aimmy.Platform.Abstractions.Interfaces;
using System.Runtime.InteropServices;

namespace Aimmy.Platform.Linux.X11.Cursor;

public sealed class X11CursorProvider : ICursorProvider, IDisposable
{
    private readonly object _sync = new();
    private IntPtr _display;
    private IntPtr _rootWindow;
    private bool _disposed;

    public X11CursorProvider()
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new InvalidOperationException("X11 cursor provider is only available on Linux.");
        }

        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to open X11 display.");
        }

        _rootWindow = XDefaultRootWindow(_display);
    }

    public static bool IsSupported(Func<string, string?>? environmentVariableReader = null)
    {
        var envReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
        var sessionType = envReader("XDG_SESSION_TYPE") ?? string.Empty;
        var display = envReader("DISPLAY") ?? string.Empty;
        var isX11 = string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(display);
        if (!isX11)
        {
            return false;
        }

        try
        {
            var probeDisplay = XOpenDisplay(IntPtr.Zero);
            if (probeDisplay == IntPtr.Zero)
            {
                return false;
            }

            XCloseDisplay(probeDisplay);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryGetPosition(out int screenX, out int screenY)
    {
        lock (_sync)
        {
            if (_disposed || _display == IntPtr.Zero)
            {
                screenX = 0;
                screenY = 0;
                return false;
            }

            var success = XQueryPointer(
                _display,
                _rootWindow,
                out _,
                out _,
                out var rootX,
                out var rootY,
                out _,
                out _,
                out _);

            screenX = rootX;
            screenY = rootY;
            return success != 0;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_display != IntPtr.Zero)
            {
                try
                {
                    XCloseDisplay(_display);
                }
                catch
                {
                    // Ignore dispose-time close failures.
                }
                finally
                {
                    _display = IntPtr.Zero;
                    _rootWindow = IntPtr.Zero;
                }
            }
        }
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

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
}
