using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Aimmy.Platform.Linux.X11.Capture;

public sealed class X11CaptureBackend : ICaptureBackend
{
    private const int ZPixmap = 2;
    private const ulong AllPlanesMask = ulong.MaxValue;
    private const int LsbFirst = 0;

    private readonly Func<string, string?> _environmentVariableReader;
    private readonly object _sync = new();

    private IntPtr _display;
    private IntPtr _rootWindow;
    private int _screen;
    private bool _closed;

    public X11CaptureBackend(AimmyConfig config, Func<string, string?>? environmentVariableReader = null)
    {
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;

        if (!IsSupported(_environmentVariableReader, out var reason))
        {
            throw new InvalidOperationException(reason);
        }

        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to open X11 display.");
        }

        _screen = XDefaultScreen(_display);
        _rootWindow = XDefaultRootWindow(_display);
    }

    public string Name => "X11NativeCapture";

    public static bool IsSupported(
        Func<string, string?> environmentVariableReader,
        out string reason)
    {
        if (!OperatingSystem.IsLinux())
        {
            reason = "Native X11 capture is only supported on Linux.";
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
            reason = "Native X11 capture is supported.";
            return true;
        }
        catch (DllNotFoundException)
        {
            reason = "libX11 is not available.";
            return false;
        }
        catch (Exception ex)
        {
            reason = $"X11 capture probe failed: {ex.Message}";
            return false;
        }
    }

    public Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_closed || _display == IntPtr.Zero)
            {
                throw new InvalidOperationException("X11 capture backend is not available.");
            }

            var displayWidth = Math.Max(1, XDisplayWidth(_display, _screen));
            var displayHeight = Math.Max(1, XDisplayHeight(_display, _screen));

            var x = Math.Clamp(region.X, 0, displayWidth - 1);
            var y = Math.Clamp(region.Y, 0, displayHeight - 1);
            var width = Math.Clamp(region.Width, 1, displayWidth - x);
            var height = Math.Clamp(region.Height, 1, displayHeight - y);

            IntPtr imagePtr = IntPtr.Zero;
            XImageNative nativeImage = default;

            try
            {
                imagePtr = XGetImage(
                    _display,
                    _rootWindow,
                    x,
                    y,
                    (uint)width,
                    (uint)height,
                    AllPlanesMask,
                    ZPixmap);

                if (imagePtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException("XGetImage returned null.");
                }

                nativeImage = Marshal.PtrToStructure<XImageNative>(imagePtr);
                if (nativeImage.Data == IntPtr.Zero)
                {
                    throw new InvalidOperationException("XGetImage returned an empty image buffer.");
                }

                var image = ConvertToImage(nativeImage, cancellationToken);
                return Task.FromResult(image);
            }
            finally
            {
                if (imagePtr != IntPtr.Zero)
                {
                    DestroyXImage(imagePtr, nativeImage);
                }
            }
        }
    }

    ~X11CaptureBackend()
    {
        CloseDisplay();
    }

    private Image<Rgba32> ConvertToImage(XImageNative nativeImage, CancellationToken cancellationToken)
    {
        var width = nativeImage.Width;
        var height = nativeImage.Height;
        var bytesPerLine = nativeImage.BytesPerLine;
        var bitsPerPixel = nativeImage.BitsPerPixel;
        var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);

        var totalBytes = checked(bytesPerLine * height);
        var buffer = new byte[totalBytes];
        Marshal.Copy(nativeImage.Data, buffer, 0, totalBytes);

        var output = new Image<Rgba32>(width, height);

        for (var py = 0; py < height; py++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowStart = py * bytesPerLine;

            for (var px = 0; px < width; px++)
            {
                var offset = rowStart + (px * bytesPerPixel);
                if (offset + bytesPerPixel > buffer.Length)
                {
                    continue;
                }

                var pixelValue = ReadPixel(buffer, offset, bytesPerPixel, nativeImage.ByteOrder);
                var r = ExtractColor(pixelValue, nativeImage.RedMask);
                var g = ExtractColor(pixelValue, nativeImage.GreenMask);
                var b = ExtractColor(pixelValue, nativeImage.BlueMask);

                output[px, py] = new Rgba32(r, g, b, 255);
            }
        }

        return output;
    }

    private static ulong ReadPixel(byte[] buffer, int offset, int bytesPerPixel, int byteOrder)
    {
        ulong value = 0;
        if (byteOrder == LsbFirst)
        {
            for (var i = 0; i < bytesPerPixel; i++)
            {
                value |= (ulong)buffer[offset + i] << (i * 8);
            }
        }
        else
        {
            for (var i = 0; i < bytesPerPixel; i++)
            {
                value <<= 8;
                value |= buffer[offset + i];
            }
        }

        return value;
    }

    private static byte ExtractColor(ulong pixelValue, ulong mask)
    {
        if (mask == 0)
        {
            return 0;
        }

        var shift = BitOperations.TrailingZeroCount(mask);
        var max = mask >> shift;
        if (max == 0)
        {
            return 0;
        }

        var component = (pixelValue & mask) >> shift;
        if (max == 255)
        {
            return (byte)component;
        }

        return (byte)((component * 255 + (max / 2)) / max);
    }

    private static void DestroyXImage(IntPtr imagePtr, XImageNative nativeImage)
    {
        try
        {
            if (nativeImage.DestroyImage != IntPtr.Zero)
            {
                var destroy = Marshal.GetDelegateForFunctionPointer<XImageDestroyDelegate>(nativeImage.DestroyImage);
                _ = destroy(imagePtr);
                return;
            }
        }
        catch
        {
            // Fall back to manual free below.
        }

        try
        {
            if (nativeImage.Data != IntPtr.Zero)
            {
                _ = XFree(nativeImage.Data);
            }
        }
        catch
        {
            // Ignore fallback free errors.
        }

        try
        {
            _ = XFree(imagePtr);
        }
        catch
        {
            // Ignore fallback free errors.
        }
    }

    private void CloseDisplay()
    {
        lock (_sync)
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            if (_display != IntPtr.Zero)
            {
                try
                {
                    _ = XCloseDisplay(_display);
                }
                catch
                {
                    // Ignore dispose-time X11 close errors.
                }
                finally
                {
                    _display = IntPtr.Zero;
                    _rootWindow = IntPtr.Zero;
                }
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XImageDestroyDelegate(IntPtr image);

    [StructLayout(LayoutKind.Sequential)]
    private struct XImageNative
    {
        public int Width;
        public int Height;
        public int XOffset;
        public int Format;
        public IntPtr Data;
        public int ByteOrder;
        public int BitmapUnit;
        public int BitmapBitOrder;
        public int BitmapPad;
        public int Depth;
        public int BytesPerLine;
        public int BitsPerPixel;
        public ulong RedMask;
        public ulong GreenMask;
        public ulong BlueMask;
        public IntPtr ObData;
        public IntPtr CreateImage;
        public IntPtr DestroyImage;
        public IntPtr GetPixel;
        public IntPtr PutPixel;
        public IntPtr SubImage;
        public IntPtr AddPixel;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XDefaultScreen(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XDisplayWidth(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern int XDisplayHeight(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetImage(
        IntPtr display,
        IntPtr drawable,
        int x,
        int y,
        uint width,
        uint height,
        ulong planeMask,
        int format);

    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);
}
