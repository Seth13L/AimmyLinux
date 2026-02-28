using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.ViewModels;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class DisplaySelectionViewModelTests
{
    [Fact]
    public void Load_PrefersMatchedDisplay_WhenConfigMatchesDiscoveredDisplay()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.DisplayWidth = 2560;
        config.Capture.DisplayHeight = 1440;
        config.Capture.DisplayOffsetX = 1920;
        config.Capture.DisplayOffsetY = 0;

        var displays = new[]
        {
            new DisplayInfo("HDMI-0", "HDMI-0", true, 0, 0, 1920, 1080),
            new DisplayInfo("DP-1", "DP-1", false, 1920, 0, 2560, 1440)
        };

        var vm = new DisplaySelectionViewModel();
        vm.Load(config, displays.Select(Aimmy.UI.Avalonia.Models.DisplayOptionModel.FromDisplayInfo));

        Assert.True(vm.UseDiscoveredDisplay);
        Assert.Equal("DP-1", vm.SelectedDisplayId);
    }

    [Fact]
    public void Apply_UsesSelectedDisplay_WhenEnabled()
    {
        var config = AimmyConfig.CreateDefault();
        var vm = new DisplaySelectionViewModel();

        vm.DisplayOptions.AddRange(new[]
        {
            Aimmy.UI.Avalonia.Models.DisplayOptionModel.FromDisplayInfo(
                new DisplayInfo("HDMI-0", "HDMI-0", true, 0, 0, 1920, 1080)),
            Aimmy.UI.Avalonia.Models.DisplayOptionModel.FromDisplayInfo(
                new DisplayInfo("DP-1", "DP-1", false, 1920, 0, 2560, 1440, 1.25f, 1.25f))
        });
        vm.UseDiscoveredDisplay = true;
        vm.SelectedDisplayId = "DP-1";

        vm.Apply(config);

        Assert.Equal(2560, config.Capture.DisplayWidth);
        Assert.Equal(1440, config.Capture.DisplayHeight);
        Assert.Equal(1920, config.Capture.DisplayOffsetX);
        Assert.Equal(0, config.Capture.DisplayOffsetY);
        Assert.Equal(1.25, config.Capture.DpiScaleX, 3);
        Assert.Equal(1.25, config.Capture.DpiScaleY, 3);
    }

    [Fact]
    public void Apply_UsesManualValues_WhenDiscoveryDisabled()
    {
        var config = AimmyConfig.CreateDefault();
        var vm = new DisplaySelectionViewModel
        {
            UseDiscoveredDisplay = false,
            DisplayWidth = 3440,
            DisplayHeight = 1440,
            DisplayOffsetX = -1920,
            DisplayOffsetY = 0,
            DpiScaleX = 1.5,
            DpiScaleY = 1.5
        };

        vm.Apply(config);

        Assert.Equal(3440, config.Capture.DisplayWidth);
        Assert.Equal(1440, config.Capture.DisplayHeight);
        Assert.Equal(-1920, config.Capture.DisplayOffsetX);
        Assert.Equal(0, config.Capture.DisplayOffsetY);
        Assert.Equal(1.5, config.Capture.DpiScaleX, 3);
        Assert.Equal(1.5, config.Capture.DpiScaleY, 3);
    }

    [Fact]
    public void Apply_UpdatesCaptureMethodAndExternalPreference()
    {
        var config = AimmyConfig.CreateDefault();
        var vm = new DisplaySelectionViewModel
        {
            UseDiscoveredDisplay = false,
            DisplayWidth = 1920,
            DisplayHeight = 1080,
            CaptureMethod = CaptureMethod.X11Shm.ToString(),
            ExternalBackendPreference = "maim"
        };

        vm.Apply(config);

        Assert.Equal(CaptureMethod.X11Shm, config.Capture.Method);
        Assert.Equal("maim", config.Capture.ExternalBackendPreference);
    }
}
