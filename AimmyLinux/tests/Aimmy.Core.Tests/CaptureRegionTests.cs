using Aimmy.Platform.Abstractions.Models;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class CaptureRegionTests
{
    [Fact]
    public void Centered_WithDisplayOffsets_ReturnsRegionInTargetDisplaySpace()
    {
        var region = CaptureRegion.Centered(
            displayWidth: 3200,
            displayHeight: 2160,
            width: 800,
            height: 960,
            displayOffsetX: 1920,
            displayOffsetY: 0);

        Assert.Equal(3120, region.X);
        Assert.Equal(600, region.Y);
        Assert.Equal(800, region.Width);
        Assert.Equal(960, region.Height);
    }
}
