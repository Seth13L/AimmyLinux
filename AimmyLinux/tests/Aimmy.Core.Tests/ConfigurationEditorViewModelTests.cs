using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.ViewModels;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class ConfigurationEditorViewModelTests
{
    [Fact]
    public void Apply_UpdatesDisplayAndDataCollectionSections()
    {
        var config = AimmyConfig.CreateDefault();
        var vm = new ConfigurationEditorViewModel();
        var displays = new[]
        {
            new DisplayInfo("HDMI-0", "HDMI-0", true, 0, 0, 1920, 1080),
            new DisplayInfo("DP-1", "DP-1", false, 1920, 0, 2560, 1440, 1.25f, 1.25f)
        };

        vm.Load(config, displays);
        vm.DisplaySelection.UseDiscoveredDisplay = true;
        vm.DisplaySelection.SelectedDisplayId = "DP-1";
        vm.DataCollection.CollectDataWhilePlaying = true;
        vm.DataCollection.AutoLabelData = true;
        vm.DataCollection.ImagesDirectory = "session/images";
        vm.DataCollection.LabelsDirectory = "session/labels";

        vm.Apply(config);

        Assert.Equal(2560, config.Capture.DisplayWidth);
        Assert.Equal(1440, config.Capture.DisplayHeight);
        Assert.Equal(1920, config.Capture.DisplayOffsetX);
        Assert.Equal(1.25, config.Capture.DpiScaleX, 3);
        Assert.True(config.DataCollection.CollectDataWhilePlaying);
        Assert.True(config.DataCollection.AutoLabelData);
        Assert.Equal("session/images", config.DataCollection.ImagesDirectory);
        Assert.Equal("session/labels", config.DataCollection.LabelsDirectory);
    }
}
