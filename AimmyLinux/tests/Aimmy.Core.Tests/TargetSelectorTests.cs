using Aimmy.Core.Config;
using Aimmy.Core.Models;
using Aimmy.Core.Targeting;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class TargetSelectorTests
{
    [Fact]
    public void ClosestToTarget_ReturnsNearestDetectionWithinFov()
    {
        var config = AimmyConfig.CreateDefault();
        config.Fov.Enabled = true;
        config.Fov.Size = 640;
        config.Model.ConfidenceThreshold = 0.1f;

        var detections = new List<Detection>
        {
            new(320, 320, 50, 50, 0.95f, 0, "enemy"),
            new(580, 580, 50, 50, 0.95f, 0, "enemy")
        };

        var selected = TargetSelector.ClosestToTarget(detections, 320, 320, config, 640, 640);

        Assert.True(selected.HasValue);
        Assert.Equal(320, selected.Value.CenterX);
        Assert.Equal(320, selected.Value.CenterY);
    }

    [Fact]
    public void ClosestToTarget_RespectsDynamicFovOverride()
    {
        var config = AimmyConfig.CreateDefault();
        config.Fov.Enabled = true;
        config.Fov.Size = 640;
        config.Model.ConfidenceThreshold = 0.1f;

        var detections = new List<Detection>
        {
            new(320, 320, 40, 40, 0.95f, 0, "enemy"),
            new(500, 320, 40, 40, 0.95f, 0, "enemy")
        };

        var selectedBase = TargetSelector.ClosestToTarget(detections, 500, 320, config, 640, 640);
        Assert.True(selectedBase.HasValue);
        Assert.Equal(500, selectedBase.Value.CenterX);

        var selectedDynamic = TargetSelector.ClosestToTarget(detections, 500, 320, config, 640, 640, fovSizeOverride: 200);
        Assert.True(selectedDynamic.HasValue);
        Assert.Equal(320, selectedDynamic.Value.CenterX);
    }
}
