using Aimmy.Core.Config;
using Aimmy.Core.Models;
using Aimmy.Core.Movement;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class StickyAimTrackerTests
{
    [Fact]
    public void Resolve_WhenStickyAimDisabled_ReturnsCurrentCandidate()
    {
        var config = AimmyConfig.CreateDefault();
        config.Aim.StickyAimEnabled = false;

        var previous = new Detection(100, 100, 30, 40, 0.95f, 0, "enemy");
        var candidate = new Detection(300, 200, 30, 40, 0.97f, 0, "enemy");

        var resolved = StickyAimTracker.Resolve(previous, candidate, new[] { candidate }, config);

        Assert.Equal(candidate, resolved);
    }

    [Fact]
    public void Resolve_ReacquiresNearestDetectionAroundPrevious_WhenCandidateJumps()
    {
        var config = AimmyConfig.CreateDefault();
        config.Aim.StickyAimEnabled = true;
        config.Aim.StickyAimThreshold = 60;

        var previous = new Detection(200, 200, 30, 40, 0.95f, 0, "enemy");
        var farCandidate = new Detection(600, 500, 30, 40, 0.98f, 0, "enemy");
        var nearPrevious = new Detection(215, 210, 30, 40, 0.96f, 0, "enemy");

        var resolved = StickyAimTracker.Resolve(previous, farCandidate, new[] { farCandidate, nearPrevious }, config);

        Assert.Equal(nearPrevious, resolved);
    }

    [Fact]
    public void Resolve_DropsTarget_WhenNoNearbyDetectionExistsAndCandidateMissing()
    {
        var config = AimmyConfig.CreateDefault();
        config.Aim.StickyAimEnabled = true;
        config.Aim.StickyAimThreshold = 40;

        var previous = new Detection(300, 300, 30, 40, 0.95f, 0, "enemy");

        var resolved = StickyAimTracker.Resolve(previous, null, Array.Empty<Detection>(), config);

        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_SwitchesToCandidate_WhenNoNearbyStickyTargetRemains()
    {
        var config = AimmyConfig.CreateDefault();
        config.Aim.StickyAimEnabled = true;
        config.Aim.StickyAimThreshold = 30;

        var previous = new Detection(300, 300, 30, 40, 0.95f, 0, "enemy");
        var newCandidate = new Detection(500, 500, 30, 40, 0.97f, 0, "enemy");

        var resolved = StickyAimTracker.Resolve(previous, newCandidate, new[] { newCandidate }, config);

        Assert.Equal(newCandidate, resolved);
    }
}
