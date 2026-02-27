using Aimmy.Platform.Linux.X11.Config;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LegacyCfgMigratorTests
{
    [Fact]
    public void Migrates_LegacyCfgPayload()
    {
        var migrator = new LegacyCfgMigrator();
        var payload = "{\"Aim Assist\":true,\"Prediction Method\":\"Shall0e's Prediction\",\"FOV Size\":500,\"AI Minimum Confidence\":55}";

        var success = migrator.TryMigrate("test.cfg", payload, out var config, out var message);

        Assert.True(success, message);
        Assert.True(config.Aim.Enabled);
        Assert.Equal(500, config.Fov.Size);
        Assert.Equal(0.55f, config.Model.ConfidenceThreshold, 3);
    }
}
