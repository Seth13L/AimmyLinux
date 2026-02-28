using Aimmy.Platform.Linux.X11.Config;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LegacyCfgMigratorTests
{
    [Fact]
    public void Migrates_LegacyCfgPayload()
    {
        var migrator = new LegacyCfgMigrator();
        var payload = "{\"Aim Assist\":true,\"Prediction Method\":\"Shall0e's Prediction\",\"FOV Size\":500,\"AI Minimum Confidence\":55,\"Display Width\":2560,\"Display Height\":1440,\"Display Offset X\":1920,\"Display Offset Y\":0,\"DPI Scale X\":1.25,\"DPI Scale Y\":1.50,\"Enable Model Switch Keybind\":false,\"Detected Player Color\":\"#FF00FF00\",\"AI Confidence Font Size\":18,\"Border Thickness\":2.5,\"Corner Radius\":12,\"UI TopMost\":true,\"StreamGuard\":true}";

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
        Assert.False(config.Input.EnableModelSwitchKeybind);
        Assert.Equal("#FF00FF00", config.Overlay.DetectedPlayerColor);
        Assert.Equal(18, config.Overlay.ConfidenceFontSize);
        Assert.Equal(2.5, config.Overlay.BorderThickness, 3);
        Assert.Equal(12, config.Overlay.CornerRadius);
        Assert.True(config.Runtime.UiTopMost);
        Assert.True(config.Runtime.StreamGuardEnabled);
    }
}
