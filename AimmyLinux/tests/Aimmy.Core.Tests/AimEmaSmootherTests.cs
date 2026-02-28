using Aimmy.Core.Movement;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class AimEmaSmootherTests
{
    [Fact]
    public void Apply_WhenDisabled_ReturnsRawValues()
    {
        var smoother = new AimEmaSmoother();

        var output = smoother.Apply(40, -20, enabled: false, smoothingAmount: 0.5);

        Assert.Equal(40, output.Dx);
        Assert.Equal(-20, output.Dy);
    }

    [Fact]
    public void Apply_WhenEnabled_UsesEmaAcrossFrames()
    {
        var smoother = new AimEmaSmoother();

        var first = smoother.Apply(100, 0, enabled: true, smoothingAmount: 0.5);
        var second = smoother.Apply(0, 0, enabled: true, smoothingAmount: 0.5);

        Assert.Equal(100, first.Dx);
        Assert.Equal(50, second.Dx);
    }
}
