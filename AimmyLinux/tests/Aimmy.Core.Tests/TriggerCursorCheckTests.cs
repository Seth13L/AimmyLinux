using Aimmy.Core.Models;
using Aimmy.Core.Trigger;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class TriggerCursorCheckTests
{
    [Fact]
    public void IsCrosshairInside_ReturnsTrue_WhenCrosshairInsideDetection()
    {
        var detection = new Detection(320, 320, 80, 80, 0.9f, 0, "enemy");

        var inside = TriggerCursorCheck.IsCrosshairInside(detection, 640, 640);

        Assert.True(inside);
    }

    [Fact]
    public void IsCrosshairInside_ReturnsFalse_WhenCrosshairOutsideDetection()
    {
        var detection = new Detection(520, 520, 60, 60, 0.9f, 0, "enemy");

        var inside = TriggerCursorCheck.IsCrosshairInside(detection, 640, 640);

        Assert.False(inside);
    }

    [Fact]
    public void IsCursorInside_IncludesDetectionBoundaries()
    {
        var detection = new Detection(100, 100, 40, 40, 0.9f, 0, "enemy");

        var leftTopInside = TriggerCursorCheck.IsCursorInside(detection, 80, 80);
        var rightBottomInside = TriggerCursorCheck.IsCursorInside(detection, 120, 120);

        Assert.True(leftTopInside);
        Assert.True(rightBottomInside);
    }
}
