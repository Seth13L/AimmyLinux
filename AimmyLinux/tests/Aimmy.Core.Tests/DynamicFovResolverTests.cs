using Aimmy.Core.Config;
using Aimmy.Core.Targeting;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class DynamicFovResolverTests
{
    [Fact]
    public void Resolve_ReturnsBaseFov_WhenDynamicFovDisabled()
    {
        var config = AimmyConfig.CreateDefault();
        config.Fov.Enabled = true;
        config.Fov.Size = 500;
        config.Fov.DynamicSize = 180;
        config.Aim.DynamicFovEnabled = false;

        var resolved = DynamicFovResolver.Resolve(config, dynamicFovKeyPressed: true);

        Assert.Equal(500, resolved);
    }

    [Fact]
    public void Resolve_ReturnsDynamicFov_WhenEnabledAndHotkeyPressed()
    {
        var config = AimmyConfig.CreateDefault();
        config.Fov.Enabled = true;
        config.Fov.Size = 500;
        config.Fov.DynamicSize = 180;
        config.Aim.DynamicFovEnabled = true;

        var resolved = DynamicFovResolver.Resolve(config, dynamicFovKeyPressed: true);

        Assert.Equal(180, resolved);
    }

    [Fact]
    public void Resolve_ReturnsBaseFov_WhenFovSystemDisabled()
    {
        var config = AimmyConfig.CreateDefault();
        config.Fov.Enabled = false;
        config.Fov.Size = 500;
        config.Fov.DynamicSize = 180;
        config.Aim.DynamicFovEnabled = true;

        var resolved = DynamicFovResolver.Resolve(config, dynamicFovKeyPressed: true);

        Assert.Equal(500, resolved);
    }
}
