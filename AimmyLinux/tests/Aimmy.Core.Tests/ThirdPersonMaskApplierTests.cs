using Aimmy.Core.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class ThirdPersonMaskApplierTests
{
    [Fact]
    public void Apply_BlacksBottomLeftQuadrantOnly()
    {
        using var frame = new Image<Rgba32>(8, 8, new Rgba32(255, 255, 255, 255));

        ThirdPersonMaskApplier.Apply(frame);

        Assert.Equal(new Rgba32(0, 0, 0, 255), frame[0, 7]);
        Assert.Equal(new Rgba32(0, 0, 0, 255), frame[3, 4]);
        Assert.Equal(new Rgba32(255, 255, 255, 255), frame[7, 7]);
        Assert.Equal(new Rgba32(255, 255, 255, 255), frame[0, 0]);
    }
}
