using Aimmy.Core.Capture;
using Aimmy.Core.Config;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class CaptureGeometryResolverTests
{
    [Fact]
    public void Resolve_DefaultConfig_CentersCaptureOnPrimaryDisplay()
    {
        var capture = AimmyConfig.CreateDefault().Capture;

        var geometry = CaptureGeometryResolver.Resolve(capture);

        Assert.Equal(1920, geometry.DisplayWidth);
        Assert.Equal(1080, geometry.DisplayHeight);
        Assert.Equal(640, geometry.CaptureWidth);
        Assert.Equal(640, geometry.CaptureHeight);
        Assert.Equal(640, geometry.CaptureX);
        Assert.Equal(220, geometry.CaptureY);
    }

    [Fact]
    public void Resolve_WithDisplayOffsetAndDpiScale_AdjustsDisplayAndCaptureCoordinates()
    {
        var capture = AimmyConfig.CreateDefault().Capture;
        capture.DisplayWidth = 2560;
        capture.DisplayHeight = 1440;
        capture.DisplayOffsetX = 1920;
        capture.DisplayOffsetY = 0;
        capture.Width = 640;
        capture.Height = 640;
        capture.DpiScaleX = 1.25;
        capture.DpiScaleY = 1.5;

        var geometry = CaptureGeometryResolver.Resolve(capture);

        Assert.Equal(3200, geometry.DisplayWidth);
        Assert.Equal(2160, geometry.DisplayHeight);
        Assert.Equal(800, geometry.CaptureWidth);
        Assert.Equal(960, geometry.CaptureHeight);
        Assert.Equal(3120, geometry.CaptureX);
        Assert.Equal(600, geometry.CaptureY);
        Assert.Equal(1.5f, geometry.FovScale, 3);
    }

    [Fact]
    public void Resolve_ClampsCaptureSize_WhenCaptureExceedsDisplay()
    {
        var capture = AimmyConfig.CreateDefault().Capture;
        capture.DisplayWidth = 1280;
        capture.DisplayHeight = 720;
        capture.Width = 2000;
        capture.Height = 2000;

        var geometry = CaptureGeometryResolver.Resolve(capture);

        Assert.Equal(1280, geometry.CaptureWidth);
        Assert.Equal(720, geometry.CaptureHeight);
        Assert.Equal(0, geometry.CaptureX);
        Assert.Equal(0, geometry.CaptureY);
    }
}
