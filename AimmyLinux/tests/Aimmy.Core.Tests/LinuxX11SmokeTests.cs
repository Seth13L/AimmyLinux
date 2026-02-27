using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Core.Models;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Aimmy.Platform.Linux.X11.Overlay;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LinuxX11SmokeTests
{
    [Fact]
    [Trait("Category", "LinuxIntegration")]
    public async Task NativeCapture_CaptureAsync_ReturnsFrameWithRequestedSize()
    {
        if (!ShouldRun())
        {
            return;
        }

        var config = BuildLinuxConfig();
        var backend = new X11CaptureBackend(config);
        var region = CaptureRegion.Centered(
            config.Capture.DisplayWidth,
            config.Capture.DisplayHeight,
            config.Capture.Width,
            config.Capture.Height);

        using var image = await backend.CaptureAsync(region, CancellationToken.None);

        Assert.Equal(config.Capture.Width, image.Width);
        Assert.Equal(config.Capture.Height, image.Height);
    }

    [Fact]
    [Trait("Category", "LinuxIntegration")]
    public async Task X11Hotkeys_StartAndPoll_DoesNotThrow()
    {
        if (!ShouldRun())
        {
            return;
        }

        var config = BuildLinuxConfig();
        await using var backend = new X11HotkeyBackend(config);

        using var cts = new CancellationTokenSource();
        await backend.StartAsync(cts.Token);

        _ = backend.IsPressed("Aim Keybind");
        _ = backend.IsPressed("Second Aim Keybind");

        cts.Cancel();
    }

    [Fact]
    [Trait("Category", "LinuxIntegration")]
    public async Task X11Overlay_ShowFovAndDetections_DoesNotThrow()
    {
        if (!ShouldRun())
        {
            return;
        }

        var config = BuildLinuxConfig();
        config.Overlay.ShowConfidence = true;
        config.Overlay.ShowTracers = true;
        config.Overlay.TracerPosition = "Bottom";

        await using var backend = new X11OverlayBackend(config);

        await backend.ShowFovAsync(320, "Circle", "#FF00AA00", CancellationToken.None);
        await backend.ShowDetectionsAsync(
            new[]
            {
                new Detection(200, 180, 80, 120, 0.92f, 0, "enemy")
            },
            CancellationToken.None);

        await Task.Delay(100);
        await backend.ClearDetectionsAsync(CancellationToken.None);
        await backend.HideFovAsync(CancellationToken.None);
    }

    private static bool ShouldRun()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var enabled = Environment.GetEnvironmentVariable("RUN_LINUX_X11_INTEGRATION");
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
    }

    private static AimmyConfig BuildLinuxConfig()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Shm;
        config.Capture.DisplayWidth = 1920;
        config.Capture.DisplayHeight = 1080;
        config.Capture.Width = 640;
        config.Capture.Height = 640;
        config.Input.AimKeybind = "Right";
        config.Input.SecondaryAimKeybind = "LeftAlt";
        return config;
    }
}
