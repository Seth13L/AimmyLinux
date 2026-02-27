using Aimmy.Platform.Linux.X11.Config;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LegacyCfgMigratorTests
{
    [Fact]
    public void Migrates_LegacyCfgPayload()
    {
        var migrator = new LegacyCfgMigrator();
        var payload = "{\"Aim Assist\":true,\"Prediction Method\":\"Shall0e's Prediction\",\"FOV Size\":500,\"AI Minimum Confidence\":55,\"Display Width\":2560,\"Display Height\":1440,\"Display Offset X\":1920,\"Display Offset Y\":0,\"DPI Scale X\":1.25,\"DPI Scale Y\":1.50}";

        var success = migrator.TryMigrate("test.cfg", payload, out var config, out var message);

        Assert.True(success, message);
        Assert.True(config.Aim.Enabled);
        Assert.Equal(500, config.Fov.Size);
        Assert.Equal(0.55f, config.Model.ConfidenceThreshold, 3);
        Assert.Equal(2560, config.Capture.DisplayWidth);
        Assert.Equal(1440, config.Capture.DisplayHeight);
        Assert.Equal(1920, config.Capture.DisplayOffsetX);
        Assert.Equal(0, config.Capture.DisplayOffsetY);
        Assert.Equal(1.25, config.Capture.DpiScaleX, 3);
        Assert.Equal(1.5, config.Capture.DpiScaleY, 3);
    }
}
