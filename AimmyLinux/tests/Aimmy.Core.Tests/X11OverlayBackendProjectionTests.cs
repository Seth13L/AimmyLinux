using Aimmy.Core.Models;
using Aimmy.Platform.Linux.X11.Overlay;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class X11OverlayBackendProjectionTests
{
    [Fact]
    public void ProjectDetectionToScreen_OffsetsByCaptureOrigin()
    {
        var detection = new Detection(100, 120, 40, 20, 0.9f, 0, "enemy");

        var rect = X11OverlayBackend.ProjectDetectionToScreen(
            detection,
            captureOffsetX: 640,
            captureOffsetY: 220,
            displayWidth: 1920,
            displayHeight: 1080);

        Assert.Equal(720, rect.X1);
        Assert.Equal(330, rect.Y1);
        Assert.Equal(760, rect.X2);
        Assert.Equal(350, rect.Y2);
    }

    [Fact]
    public void ProjectDetectionToScreen_ClampsToDisplayBounds()
    {
        var detection = new Detection(10, 10, 100, 100, 0.8f, 0, "enemy");

        var rect = X11OverlayBackend.ProjectDetectionToScreen(
            detection,
            captureOffsetX: -20,
            captureOffsetY: -20,
            displayWidth: 80,
            displayHeight: 60);

        Assert.Equal(0, rect.X1);
        Assert.Equal(0, rect.Y1);
        Assert.InRange(rect.X2, 1, 79);
        Assert.InRange(rect.Y2, 1, 59);
    }

    [Fact]
    public void ProjectDetectionToScreen_AppliesDpiScaleAndDisplayOrigin()
    {
        var detection = new Detection(100, 120, 40, 20, 0.9f, 0, "enemy");

        var rect = X11OverlayBackend.ProjectDetectionToScreen(
            detection,
            captureOffsetX: 3120,
            captureOffsetY: 600,
            displayWidth: 3200,
            displayHeight: 2160,
            dpiScaleX: 1.25f,
            dpiScaleY: 1.5f,
            displayOriginX: 1920,
            displayOriginY: 0);

        Assert.Equal(3220, rect.X1);
        Assert.Equal(765, rect.Y1);
        Assert.Equal(3270, rect.X2);
        Assert.Equal(795, rect.Y2);
    }
}
